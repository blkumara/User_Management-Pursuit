using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System;
/* =========================================================
    Item Name: Validation Class-DisableFormValueModelBindingAttribute
    Author: Ortusolis for EvolveAccess Team
    Version: 1.0
    Copyright 2022 - 2023 - Evolve Access
 ============================================================ */
namespace Pursuit.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var factories = context.ValueProviderFactories;
            factories.RemoveType<FormValueProviderFactory>();
            factories.RemoveType<FormFileValueProviderFactory>();
            factories.RemoveType<JQueryFormValueProviderFactory>();
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }

    public class GenerateAntiforgeryTokenCookieAttribute : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var antiforgery = context.HttpContext.RequestServices.GetService<IAntiforgery>();

            // Send the request token as a JavaScript-readable cookie
            var tokens = antiforgery.GetAndStoreTokens(context.HttpContext);

            context.HttpContext.Response.Cookies.Append(
                "RequestVerificationToken",
                tokens.RequestToken,
                new CookieOptions() { HttpOnly = false });
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
