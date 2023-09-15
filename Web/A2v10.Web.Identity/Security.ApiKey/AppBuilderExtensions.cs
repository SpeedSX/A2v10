﻿// Copyright © 2021-2023 Oleksandr Kukhtin. All rights reserved.

using System;
using Owin;

using Microsoft.Owin.Extensions;

namespace A2v10.Web.Identity.ApiKey;

public static class ApiKeyAppBuilderExtensions
{
	public static IAppBuilder UseApiKeyAuthentication(this IAppBuilder app, ApiKeyAuthenticationOptions options = null)
	{
		if (app == null)
			throw new ArgumentNullException(nameof(app));

		options ??= new ApiKeyAuthenticationOptions();

		app.Use<ApiKeyAuthenticationMiddleware>(app, options);
		app.UseStageMarker(PipelineStage.Authenticate);

		return app;
	}
}
