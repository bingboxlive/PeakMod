using System;
using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using X509Certificate2 = System.Security.Cryptography.X509Certificates.X509Certificate2;
using X509KeyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags;
using SIPSorcery.Net;

namespace BingBox.Utils
{
    public static class CertificateUtils
    {
        public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName)
        {
            var random = new SecureRandom();
            var keyGenerationParameters = new KeyGenerationParameters(random, 2048);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            var keyPair = keyPairGenerator.GenerateKeyPair();

            var certificateGenerator = new X509V3CertificateGenerator();
            var cn = new X509Name($"CN={subjectName}");
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);

            certificateGenerator.SetSerialNumber(serialNumber);
            certificateGenerator.SetIssuerDN(cn);
            certificateGenerator.SetSubjectDN(cn);
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(1));
            certificateGenerator.SetPublicKey(keyPair.Public);

            var signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private, random);
            var certificate = certificateGenerator.Generate(signatureFactory);

            var builder = new Pkcs12StoreBuilder();
            builder.SetUseDerEncoding(true);
            var store = builder.Build();

            var certificateEntry = new X509CertificateEntry(certificate);
            store.SetCertificateEntry(subjectName, certificateEntry);
            store.SetKeyEntry(subjectName, new AsymmetricKeyEntry(keyPair.Private), new[] { certificateEntry });

            using (var stream = new MemoryStream())
            {
                store.Save(stream, "bingbox".ToCharArray(), random);
                var pfxBytes = stream.ToArray();
                pfxBytes = Pkcs12Utilities.ConvertToDefiniteLength(pfxBytes);

                return new X509Certificate2(
                    pfxBytes,
                    "bingbox",
                    X509KeyStorageFlags.Exportable
                );
            }
        }
        public static RTCCertificate2 GenerateSelfSignedRtcCertificate(string subjectName)
        {
            var random = new SecureRandom();
            var keyGenerationParameters = new KeyGenerationParameters(random, 2048);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            var keyPair = keyPairGenerator.GenerateKeyPair();

            var certificateGenerator = new X509V3CertificateGenerator();
            var cn = new X509Name($"CN={subjectName}");
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);

            certificateGenerator.SetSerialNumber(serialNumber);
            certificateGenerator.SetIssuerDN(cn);
            certificateGenerator.SetSubjectDN(cn);
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(1));
            certificateGenerator.SetPublicKey(keyPair.Public);

            var signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private, random);
            var certificate = certificateGenerator.Generate(signatureFactory);

            return new RTCCertificate2
            {
                Certificate = certificate,
                PrivateKey = keyPair.Private
            };
        }
    }
}
