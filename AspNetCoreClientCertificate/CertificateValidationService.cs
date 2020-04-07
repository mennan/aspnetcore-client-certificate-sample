using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace AspNetCoreClientCertificate
{
	public class CertificateValidationService
	{
		public bool ValidateCertificate(X509Certificate2 clientCertificate)
		{
			var cert = new X509Certificate2(Path.Combine("mkose.cer"));
			if (clientCertificate.Thumbprint == cert.Thumbprint)
			{
				return true;
			}

			return false;
		}
	}
}