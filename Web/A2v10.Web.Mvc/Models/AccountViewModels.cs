﻿// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;

namespace A2v10.Web.Mvc.Models
{
	public class SendCodeViewModel
	{
		public String SelectedProvider { get; set; }
		public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
		public String ReturnUrl { get; set; }
		public Boolean RememberMe { get; set; }
	}

	public class VerifyCodeViewModel
	{
		[Required]
		public String Provider { get; set; }

		[Required]
		[Display(Name = "Code")]
		public String Code { get; set; }
		public String ReturnUrl { get; set; }

		[Display(Name = "Remember this browser?")]
		public Boolean RememberBrowser { get; set; }

		public Boolean RememberMe { get; set; }
	}

	public class ForgotViewModel
	{
		[Required]
		public String Email { get; set; }
	}

	public class AppTitleModel
	{
		public String AppTitle { get; set; }
		public String AppSubTitle { get; set; }
	}

	public class LoginViewModel
	{
		public String Name { get; set; }
		public String Password { get; set; }
		public Boolean RememberMe { get; set; }
	}

	public class RegisterTenantModel
	{
		public Int32 Id { get; set; }
		public String Name { get; set; }
		public String Email { get; set; }
		public String Password { get; set; }
		public String PersonName { get; set; }
		public String Phone { get; set; }
		public String Referral { get; set; }
		public String Locale { get; set; }

		public ExpandoObject ExtraData { get; set; }
	}


	public class ConfirmEmailModel
	{
		public String Email { get; set; }
		public String Code { get; set; }
	}

	public class UserReferralInfo
	{
		public Int64 UserId { get; set; }
		public String Referral { get; set; }
	}

	public class RegisterViewModel
	{
		[Display(Name = "Name")]
		public String Name { get; set; }

		[EmailAddress]
		public String Email { get; set; }

		[Required]
		[DataType(DataType.Password)]
		public String Password { get; set; }

		[DataType(DataType.Password)]
		[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
		public String ConfirmPassword { get; set; }
	}

	public class NewPasswordViewModel
	{
		public String Password { get; set; }
	}

	public class ResetPasswordViewModel
	{
		public String EMail { get; set; }

		public String Password { get; set; }
		public String Confirm { get; set; }

		public String Code { get; set; }
	}

	public class ChangePasswordViewModel
	{
		public Int64 Id { get; set; }
		[DataType(DataType.Password)]
		public String OldPassword { get; set; }
		[DataType(DataType.Password)]
		public String NewPassword { get; set; }
	}

	public class ForgotPasswordViewModel
	{
		public String EMail { get; set; }
	}
}
