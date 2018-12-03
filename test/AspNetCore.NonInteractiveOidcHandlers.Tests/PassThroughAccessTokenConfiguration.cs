using System;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using NFluent;
using Xunit;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests
{
	public class PassThroughAccessTokenConfiguration
	{
		[Fact]
		public void No_TokenRetriever_should_throw()
		{
			Task Act() => WebHostFactory
				.CreateClient(b => b.AddAccessTokenPassThrough(o =>
				{
					o.TokenRetriever = null;
				}))
				.GetAsync("https://default");

			Check.ThatAsyncCode(Act)
				.Throws<InvalidOperationException>()
				.WithMessage(GetExpectedValidationErrorMessage(
					"You must set TokenRetriever."));
		}

		[Fact]
		public void With_TokenRetriever_should_not_throw()
		{
			Task Act() => WebHostFactory
				.CreateClient(b => b.AddAccessTokenPassThrough())
				.GetAsync("https://default");

			Check.ThatAsyncCode(Act).Not.ThrowsAny();
		}

		private string GetExpectedValidationErrorMessage(params string[] validationErrors)
			=> $"Options are not valid:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, validationErrors)}";
	}
}
