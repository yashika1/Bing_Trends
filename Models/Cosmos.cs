using System;
using VcClient;
using Microsoft.Search.Autopilot;
using SatoriBrowser.Common.Utilities;
using System.Security.Cryptography.X509Certificates;

namespace SatoriBrowser.Common.Cosmos
{
    public static class Cosmos
    {
        private static bool CertificateBasedAuthenticationEnabled = true;
        private static X509Certificate2 _certificate = null;

        public static void Copy(string source, string target, TimeSpan? expiration = null)
        {
            if (VC.StreamExists(target))
                VC.Delete(target);

            VC.Concatenate(source, target);

            if (expiration != null)
                VC.SetStreamExpirationTime(target, expiration.Value);
        }

        public static void Setup()
        {
            if (CertificateBasedAuthenticationEnabled && APRuntime.IsInitialized /*&& EnvironmentUtils.IsProductionOrIntEnv()*/)
                SetupApMachineFunctionCertificate();
            else if (CertificateBasedAuthenticationEnabled /*&& EnvironmentUtils.IsCloudTestEnv()*/)
                SetupCloudTestCertificate();
            else
                SetupUserCredentials();
        }

        public static X509Certificate2 GetCertificate()
        {
            return _certificate;
        }

        private static X509Certificate2 GetApMachineFunctionCertificate()
        {
            return ApPkiClient.ApLookupLocalCert(ApAuthType.Client, ApLookupFlags.MfCertOnly);
        }

        public static void SetupApMachineFunctionCertificate()
        {
            _certificate = GetApMachineFunctionCertificate();
            VC.Setup(null, _certificate);
        }

        public static void SetupUserCredentials()
        {
            VC.SetDefaultCredentials();
        }

        public static void SetupCloudTestCertificate()
        {
            // Certificate friendly name is 'kb-cloudtest'
            const string certThumbPrintId = "4E01643BFCE442FD26A0DA1C2A5079C1244E428C";

            X509Store store = new X509Store("My", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = store.Certificates;
            X509Certificate2Collection cloudTestCertificateCollection = collection.Find(X509FindType.FindByThumbprint,
                certThumbPrintId, false);

            if (cloudTestCertificateCollection.Count < 1)
            {
                throw new Exception("Could not find Cloudtest certificate");
            }

            _certificate = cloudTestCertificateCollection[0];

            VC.Setup(null, _certificate);
        }
    }
}
