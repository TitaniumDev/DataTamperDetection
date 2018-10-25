using Application.DTOs;
using Application.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Titanium.Security.Cryptography;

namespace Application.Controllers
{

    public sealed class KeyController : ControllerBase, IDisposable
    {
        #region Constructors

        public KeyController(IHostingEnvironment environment) => _environment = environment;

        #endregion

        #region non-Public Members

        private IHostingEnvironment _environment;

        private DatabaseContext _dataContext = new DatabaseContext();

        #endregion

        // GET api/key/find/{id}
        [HttpGet]
        public ActionResult<ThirdPartyPublicKeys> Find([FromRoute]int Id)
        {
            return _dataContext.ThirdPartyPublicKeys.Find(Id);
        }

        // PUT api/key/submit
        [HttpPut]
        public ActionResult Submit([FromBody] KeySubmittal keySubmittal)
        {
            var existingkeyInfo = _dataContext.ThirdPartyPublicKeys.Where(key => key.Subject == keySubmittal.Email).FirstOrDefault();

            byte[] keyData = Encoding.ASCII.GetBytes(keySubmittal.PgpKey);

            //Digital Signing using private key
            byte[] signature = null;
            using (var rsaCryptoProvider = new RsaCSP(new X509Certificate2(Path.Combine(_environment.WebRootPath, @"certs\RSA_2048_KeyPair.pfx"), "C0mput31", X509KeyStorageFlags.EphemeralKeySet)))
            {
                signature = rsaCryptoProvider.Sign(keyData);
            }

            //Signature stored with key
            var keyInfo = new ThirdPartyPublicKeys()
            {
                Subject = keySubmittal.Email,
                Key = keySubmittal.PgpKey,
                Signature = signature
            };

            // Add or Update key information
            if (existingkeyInfo != null)
            {
                existingkeyInfo.Key = keyInfo.Key;
                existingkeyInfo.Signature = keyInfo.Signature;

                _dataContext.ThirdPartyPublicKeys.Update(existingkeyInfo);

                keyInfo = existingkeyInfo;
            } else
            {                
                _dataContext.ThirdPartyPublicKeys.Add(keyInfo);
            }            

            _dataContext.SaveChanges();

            return Ok(keyInfo);
        }

        // POST api/key/tamper
        [HttpPost]
        public ActionResult Tamper([FromBody]string email)
        {
            var keyInfo = _dataContext.ThirdPartyPublicKeys.Where(key => key.Subject == email).FirstOrDefault();

            if (keyInfo == null)
                return NotFound($"No key info exists for this contact:'{email}'");

            keyInfo.Key = keyInfo.Key?.Replace('e', 'X');

            _dataContext.ThirdPartyPublicKeys.Update(keyInfo);

            _dataContext.SaveChanges();

            return Ok($"The key for the contact:'{email}' has been modified and should now fail signature verification.");
        }

        // POST api/key/verify
        [HttpPost]
        public ActionResult Verify([FromBody] string email)
        {
            var keyInfo = _dataContext.ThirdPartyPublicKeys.Where(key => key.Subject == email).FirstOrDefault();

            if (keyInfo == null)
                return NotFound($"No key info exists for this contact:'{email}'");

            //Signature verification using public key
            bool isSignatureValid = false;
            using (var rsaCryptoProvider = new RsaCSP(new X509Certificate2(Path.Combine(_environment.WebRootPath, @"certs\RSA_2048_PublicKey.cer"))))
            {
                // Only the public key is required for signature validation
                isSignatureValid = rsaCryptoProvider.VerifySignature(Encoding.ASCII.GetBytes(keyInfo.Key), keyInfo.Signature);
            }

            string result;

            if (isSignatureValid)
                result = $"Signature verification succeeded, the contact key is safe to use.";
            else
                result = $"Signature verification failed, the contact key is NOT safe to use.";

            return Ok(result);
        }

        #region IDisposable Support

        private bool disposedValue = false;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_dataContext != null)
                        _dataContext.Dispose();
                }

                _dataContext = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
