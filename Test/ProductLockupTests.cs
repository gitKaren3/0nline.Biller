using _0nline.Biller.Wasm.Components;
using _0nline.Biller.Test.Base;
using Bunit;

namespace _0nline.Biller.Test;

public class ProductLockupTests : MudTestContext
{
    [Fact]
    public void Renders_Wordmark_Separator_Product_And_Cursor()
    {
        var cut = Render<ProductLockup>(p => p.Add(x => x.Product, "e-Biller").Add(x => x.ShowWordmark, true));

        Assert.Equal("0nline", cut.Find(".wordmark").TextContent);
        Assert.Equal("▸", cut.Find(".sep").TextContent);
        Assert.Equal("e-Biller", cut.Find(".product").TextContent);
        Assert.NotNull(cut.Find(".cursor"));
    }

    [Fact]
    public void Applies_The_Class_Parameter_To_The_Root_Element()
    {
        var cut = Render<ProductLockup>(p => p
            .Add(x => x.Product, "e-Biller")
            .Add(x => x.Class, "hero-lockup"));

        Assert.Contains("hero-lockup", cut.Find(".product-lockup").ClassList);
    }

    [Fact]
    public void OnAccent_Adds_The_On_Accent_Marker_Class()
    {
        var cut = Render<ProductLockup>(p => p.Add(x => x.OnAccent, true));

        Assert.Contains("on-accent", cut.Find(".product-lockup").ClassList);
    }
}
