using _0nline.Biller.Wasm.Pages;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using _0nline.Biller.Test.Base;


namespace _0nline.Biller.Test;

public class Sample : MudTestContext, IAsyncLifetime
{
    public Sample()
    { 
      
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public void CounterStartsAtZero()
    {
        var cut =  Render<Test_Page>();
        cut.Find("p").MarkupMatches("<p>Current count: 0</p>");
    }

    [Fact]
    public void ClickingButtonIncrementsCounter()
    {
        var cut = Render<Test_Page>();
        cut.Find("button").Click();
        cut.Find("p").MarkupMatches("<p>Current count: 1</p>");
    }

}
