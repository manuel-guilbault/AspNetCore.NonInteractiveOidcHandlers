using System;
using System.Threading.Tasks;
using AspNetCore.OAuth2TokenDelegation.Tests.Util;
using NFluent;
using Xunit;

namespace AspNetCore.OAuth2TokenDelegation.Tests
{
	public class Configuration
	{
		[Fact]
		public void Empty_options_should_throw()
		{
			Task Act() => PipelineFactory.CreateClient(o => { }).GetAsync("https://upstream");

			Check.ThatAsyncCode(Act)
				.Throws<InvalidOperationException>()
				.WithMessage(GetExpectedValidationErrorMessage(
					"You must either set Authority or TokenEndpoint.",
					"You must set ClientId.",
					"You must set ClientSecret.",
					"You must set Scope."));
		}

		[Fact]
		public void No_ClientId_should_throw()
		{
			Task Act() => PipelineFactory.CreateClient(o => { o.Authority = "https://authority"; }).GetAsync("https://upstream");

			Check.ThatAsyncCode(Act)
				.Throws<InvalidOperationException>()
				.WithMessage(GetExpectedValidationErrorMessage(
					"You must set ClientId.",
					"You must set ClientSecret.",
					"You must set Scope."));
		}

		[Fact]
		public void No_ClientSecret_should_throw()
		{
			Task Act() => PipelineFactory.CreateClient(o =>
			{
				o.Authority = "https://authority";
				o.ClientId = "test-client";
			}).GetAsync("https://upstream");

			Check.ThatAsyncCode(Act)
				.Throws<InvalidOperationException>()
				.WithMessage(GetExpectedValidationErrorMessage(
					"You must set ClientSecret.",
					"You must set Scope."));
		}

		[Fact]
		public void No_Scope_should_throw()
		{
			Task Act() => PipelineFactory.CreateClient(o =>
			{
				o.Authority = "https://authority";
				o.ClientId = "test-client";
				o.ClientSecret = "test-client secret key";
			}).GetAsync("https://upstream");

			Check.ThatAsyncCode(Act)
				.Throws<InvalidOperationException>()
				.WithMessage(GetExpectedValidationErrorMessage(
					"You must set Scope."));
		}

		[Fact]
		public void No_TokenRetriever_should_throw()
		{
			Task Act() => PipelineFactory.CreateClient(o =>
			{
				o.Authority = "https://authority";
				o.ClientId = "test-client";
				o.ClientSecret = "test-client secret key";
				o.Scope = "downstream-api";
				o.TokenRetriever = null;
			}).GetAsync("https://upstream");

			Check.ThatAsyncCode(Act)
				.Throws<InvalidOperationException>()
				.WithMessage(GetExpectedValidationErrorMessage(
					"You must set TokenRetriever."));
		}

		[Fact]
		public void No_Authority_but_TokenEndpoint_should_not_throw()
		{
			Task Act() => PipelineFactory.CreateClient(o =>
			{
				o.TokenEndpoint = "https://authority/connect/token";
				o.ClientId = "test-client";
				o.ClientSecret = "test-client secret key";
				o.Scope = "downstream-api";
			}).GetAsync("https://upstream");

			Check.ThatAsyncCode(Act).Not.ThrowsAny();
		}

		[Fact]
		public void EnableCaching_but_no_caching_service_should_throw()
		{
			Task Act() => PipelineFactory.CreateClient(o =>
			{
				o.Authority = "https://authority";
				o.ClientId = "test-client";
				o.ClientSecret = "test-client secret key";
				o.Scope = "downstream-api";
				o.EnableCaching = true;
			}, addCaching: false).GetAsync("https://upstream");

			Check.ThatAsyncCode(Act)
				.Throws<InvalidOperationException>()
				.WithMessage("Caching is enabled, but no IDistributedCache is found in the services collection.");
		}

		[Fact]
		public void EnabledCaching_with_caching_service_should_not_throw()
		{
			Task Act() => PipelineFactory.CreateClient(o =>
			{
				o.TokenEndpoint = "https://authority/connect/token";
				o.ClientId = "test-client";
				o.ClientSecret = "test-client secret key";
				o.Scope = "downstream-api";
			}, addCaching: true).GetAsync("https://upstream");

			Check.ThatAsyncCode(Act).Not.ThrowsAny();
		}

		private string GetExpectedValidationErrorMessage(params string[] validationErrors)
			=> $"Options are not valid:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, validationErrors)}";
	}
}
