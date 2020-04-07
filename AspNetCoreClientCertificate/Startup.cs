using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCoreClientCertificate
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<CertificateValidationService>();

			services.AddCertificateForwarding(options =>
			{
				options.CertificateHeader = "X-SSL-CERT";
				options.HeaderConverter = (headerValue) =>
				{
					X509Certificate2 clientCertificate = null;

					if (!string.IsNullOrWhiteSpace(headerValue))
					{
						byte[] bytes = StringToByteArray(headerValue);
						clientCertificate = new X509Certificate2(bytes);
					}

					return clientCertificate;
				};
			});

			services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
				.AddCertificate(options => // code from ASP.NET Core sample
				{
					options.AllowedCertificateTypes = CertificateTypes.All;
					options.RevocationMode = X509RevocationMode.NoCheck;
					options.ValidateCertificateUse = false;

					options.Events = new CertificateAuthenticationEvents
					{
						OnCertificateValidated = context =>
						{
							var validationService =
								context.HttpContext.RequestServices.GetService<CertificateValidationService>();

							if (validationService.ValidateCertificate(context.ClientCertificate))
							{
								var claims = new[]
								{
									new Claim(ClaimTypes.NameIdentifier, context.ClientCertificate.Subject, ClaimValueTypes.String, context.Options.ClaimsIssuer),
									new Claim(ClaimTypes.Name, context.ClientCertificate.Subject, ClaimValueTypes.String, context.Options.ClaimsIssuer)
								};

								context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
								context.Success();
							}
							else
							{
								context.Fail("invalid cert");
							}

							return Task.CompletedTask;
						},
						OnAuthenticationFailed = context =>
						{
							return Task.CompletedTask;
						}
					};
				});

			services.AddAuthorization();

			services.AddControllersWithViews();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}
			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();
			app.UseCertificateForwarding();
			app.UseAuthentication();
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");
			});
		}

		private static byte[] StringToByteArray(string hex)
		{
			var numberChars = hex.Length;
			var bytes = new byte[numberChars / 2];

			for (var i = 0; i < numberChars; i += 2)
			{
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}

			return bytes;
		}
	}
}
