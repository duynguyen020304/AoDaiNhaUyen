using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class EmailTemplateServiceTests
{
  [Fact]
  public async Task RenderAsync_EncodesTokenValues_ByDefault()
  {
    await using var dbContext = CreateDbContext();
    dbContext.EmailTemplates.Add(new EmailTemplate
    {
      Key = "test.template",
      Name = "Test",
      Subject = "Xin chào {{name}}",
      HtmlBody = "<p>{{name}}</p><div>{{trustedHtml}}</div>",
      Locale = "vi-VN",
      Version = 1
    });
    await dbContext.SaveChangesAsync();

    var service = new EmailTemplateService(dbContext);

    var rendered = await service.RenderAsync(
      "test.template",
      "{\"name\":\"<script>alert(1)</script>\",\"trustedHtml\":\"<strong>OK</strong>\"}");

    Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", rendered.Subject);
    Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", rendered.HtmlBody);
    Assert.Contains("<strong>OK</strong>", rendered.HtmlBody);
  }

  [Fact]
  public async Task RenderAsync_AllowsTrustedHtmlBodyOnlyForAllowlistedBuiltIns()
  {
    await using var dbContext = CreateDbContext();
    var service = new EmailTemplateService(dbContext);

    var allowed = await service.RenderAsync(
      "order.invoice",
      "{\"subject\":\"Invoice\",\"trustedHtmlBody\":\"<h1>OK</h1>\"}");
    var blocked = await service.RenderAsync(
      "marketing.untrusted",
      "{\"subject\":\"Bad\",\"trustedHtmlBody\":\"<script>alert(1)</script>\"}");

    Assert.Contains("<h1>OK</h1>", allowed.HtmlBody);
    Assert.DoesNotContain("<script>", blocked.HtmlBody);
  }

  private static AppDbContext CreateDbContext()
  {
    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);
  }
}
