using AngleSharp.Dom.Html;

namespace IdentityBase.Public.IntegrationTests
{
    // http://www.stefanhendriks.com/2016/04/29/integration-testing-your-dot-net-core-app-with-an-in-memory-database/
    // http://www.stefanhendriks.com/2016/05/11/integration-testing-your-asp-net-core-app-dealing-with-anti-request-forgery-csrf-formdata-and-cookies/

    public static class IHtmlDocumentExtensions
    {
        public static string GetInputValue(this IHtmlDocument doc, string name)
        {
            var node = doc.QuerySelector($"input[name=\"{name}\"]");

            if (node != null)
            {
                return node.GetAttribute("value");
            }

            return null;
        }

        public static string GetAntiForgeryToken(this IHtmlDocument doc)
        {
            return doc.GetInputValue("__RequestVerificationToken"); 
        }

        public static string GetReturnUrl(this IHtmlDocument doc)
        {
            return doc.GetInputValue("ReturnUrl");
        }
    }
}
