using System.Linq.Expressions;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Client.Components;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace _0nline.Biller.Test;

public class PhoneNumberFieldTests : Base.MudTestContext, IAsyncLifetime
{
    public PhoneNumberFieldTests()
    {
        // MudBlazor awaits the JS hooks below during OnAfterRender. The shared test
        // setups match them but never complete them, so register completions here.
        JSInterop.Mode = JSRuntimeMode.Loose;
        foreach (var identifier in new[]
                 {
                     "mudElementRef.addOnBlurEvent",
                     "mudElementRef.addOnFocusEvent",
                     "mudElementRef.addOnClickEvent",
                     "mudElementRef.removeOnBlurEvent",
                     "mudElementRef.removeOnFocusEvent",
                     "mudPopover.initialize",
                     "mudPopover.open",
                     "mudPopover.close",
                     "mudPopover.connect",
                     "mudPopover.disconnect",
                     "mudResizeObserver.observe",
                     "mudResizeObserver.unobserve",
                     "mudScrollManager.scrollTo",
                     "mudKeyInterceptor.connect",
                     "mudKeyInterceptor.disconnect",
                 })
        {
            JSInterop.SetupVoid(identifier, _ => true).SetVoidResult();
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ValidNumberIsCapturedAndNotReportedAsRequired()
    {
        var contact = new Contact();

        var cut = RenderField(contact);

        var input = PhoneInput(cut);
        input.Change("0821234567");
        input.Blur(new FocusEventArgs());

        cut.WaitForAssertion(() => Assert.Equal("+27821234567", contact.CellNo));
        Assert.DoesNotContain("Required", cut.Markup);
    }

    [Fact]
    public void ExistingNumberCanBeReplaced()
    {
        var contact = new Contact { CellNo = "+27821111111" };

        var cut = RenderField(contact);

        var input = PhoneInput(cut);
        input.Change("0829999999");
        input.Blur(new FocusEventArgs());

        cut.WaitForAssertion(() => Assert.Equal("+27829999999", contact.CellNo));
        Assert.DoesNotContain("Required", cut.Markup);
    }

    [Fact]
    public void EmptyNumberStillShowsRequiredOnBlur()
    {
        var contact = new Contact();

        var cut = RenderField(contact);

        var input = PhoneInput(cut);
        input.Change(string.Empty);
        input.Blur(new FocusEventArgs());

        cut.WaitForAssertion(() => Assert.Contains("Required", cut.Markup));
        Assert.Null(contact.CellNo);
    }

    private IRenderedComponent<PhoneNumberField> RenderField(Contact contact)
        => Render<PhoneNumberField>(p => p
            .Add(x => x.Value, contact.CellNo)
            .Add(x => x.ValueChanged, v => contact.CellNo = v)
            .Add(x => x.For, (Expression<Func<string?>>)(() => contact.CellNo)));

    private static AngleSharp.Dom.IElement PhoneInput(IRenderedComponent<PhoneNumberField> cut)
        => cut.FindAll("input").Last();
}
