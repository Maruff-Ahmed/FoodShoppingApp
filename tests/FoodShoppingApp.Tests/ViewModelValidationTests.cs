using System.ComponentModel.DataAnnotations;
using FoodShoppingApp.Models;
using Xunit;

namespace FoodShoppingApp.Tests
{
    public class ViewModelValidationTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void CustomerRegisterViewModel_ValidData_PassesValidation()
        {
            var model = new CustomerRegisterViewModel
            {
                Email = "john@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var results = ValidateModel(model);

            Assert.Empty(results);
        }

        [Fact]
        public void CustomerRegisterViewModel_MismatchedPasswords_FailsValidation()
        {
            var model = new CustomerRegisterViewModel
            {
                Email = "john@example.com",
                Password = "Password123!",
                ConfirmPassword = "DifferentPassword456!"
            };

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(CustomerRegisterViewModel.ConfirmPassword)));
        }

        [Fact]
        public void CustomerRegisterViewModel_ShortPassword_FailsValidation()
        {
            var model = new CustomerRegisterViewModel
            {
                Email = "john@example.com",
                Password = "123",
                ConfirmPassword = "123"
            };

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(CustomerRegisterViewModel.Password)));
        }

        [Fact]
        public void CustomerRegisterViewModel_MissingEmail_FailsValidation()
        {
            var model = new CustomerRegisterViewModel
            {
                Email = "",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(CustomerRegisterViewModel.Email)));
        }

        [Fact]
        public void CustomerLoginViewModel_EmptyFields_FailsValidation()
        {
            var model = new CustomerLoginViewModel
            {
                Email = "",
                Password = ""
            };

            var results = ValidateModel(model);

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void AdminLoginViewModel_EmptyFields_FailsValidation()
        {
            var model = new AdminLoginViewModel
            {
                Email = "",
                Password = ""
            };

            var results = ValidateModel(model);

            Assert.Equal(2, results.Count);
        }
    }
}
