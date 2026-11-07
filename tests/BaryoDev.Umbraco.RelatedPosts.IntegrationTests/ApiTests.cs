using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Tests.Common.Testing;

namespace BaryoDev.Umbraco.RelatedPosts.IntegrationTests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ApiTests : RelatedPostsFixture
{
    private (RelatedPostsApiController Controller, HttpContext Http) Build(bool preview = false)
    {
        var umbraco = Stub.Make<IUmbracoContextAccessor>(("TryGetUmbracoContext", a =>
        {
            a[0] = preview ? Stub.Make<IUmbracoContext>(("get_InPreviewMode", _ => true)) : null;
            return preview;
        }));
        var http = new DefaultHttpContext();
        var controller = new RelatedPostsApiController(Related, Options, GetRequiredService<ILanguageService>(), umbraco)
        {
            ControllerContext = new ControllerContext(new ActionContext(http, new RouteData(), new ControllerActionDescriptor())),
        };
        return (controller, http);
    }

    [Test]
    public async Task Api_returns_404_for_an_unpublished_source()
    {
        CreatePostType();
        var root = Publish("Root");
        var draft = Content.Create("Draft", root.Id, "post");
        Content.Save(draft);
        var (controller, _) = Build();

        Assert.That(await controller.Get(draft.Key, null, null), Is.InstanceOf<NotFoundResult>());
        Assert.That(await controller.Get(Guid.NewGuid(), null, null), Is.InstanceOf<NotFoundResult>());

        var live = Publish("Live", root);
        Assert.That(await controller.Get(live.Key, null, null), Is.InstanceOf<OkObjectResult>());

        var unpublished = Content.Unpublish(live);
        Assert.That(unpublished.Success, Is.True);
        Assert.That(await controller.Get(live.Key, null, null), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Preview_response_is_private_no_store()
    {
        CreatePostType();
        var root = Publish("Root");
        var source = Publish("Source", root);

        var (preview, previewHttp) = Build(preview: true);
        Assert.That(await preview.Get(source.Key, null, null), Is.InstanceOf<OkObjectResult>());
        Assert.That(previewHttp.Response.Headers.CacheControl.ToString(), Is.EqualTo("private, no-store"));

        var (anonymous, anonymousHttp) = Build();
        Assert.That(await anonymous.Get(source.Key, null, null), Is.InstanceOf<OkObjectResult>());
        Assert.That(anonymousHttp.Response.Headers.CacheControl.ToString(), Is.EqualTo("public, max-age=60"));

        var (signedIn, signedInHttp) = Build();
        signedInHttp.Request.Headers.Authorization = "Bearer x";
        Assert.That(await signedIn.Get(source.Key, null, null), Is.InstanceOf<OkObjectResult>());
        Assert.That(signedInHttp.Response.Headers.CacheControl.ToString(), Is.EqualTo("private, no-store"));
    }
}
