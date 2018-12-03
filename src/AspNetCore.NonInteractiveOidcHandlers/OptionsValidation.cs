using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public interface IValidatableOptions
	{
		IEnumerable<string> GetValidationErrors();
	}

	public static class OptionsValidationExtension
	{
		/// <summary>
		/// Check that the options are valid. Should throw an InvalidOperationException if things are not ok.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		public static void Validate(this IValidatableOptions options)
		{
			var validationErrors = options.GetValidationErrors().ToList();
			if (validationErrors.Any())
			{
				throw new InvalidOperationException(
					$"Options are not valid:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, validationErrors)}");
			}
		}
	}
}
