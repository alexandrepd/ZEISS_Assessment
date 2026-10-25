using Microsoft.EntityFrameworkCore;
using ZEISS.ProductManagement.Infrastructure.Persistence;

namespace ZEISS.ProductManagement.IntegrationTests;

public sealed class ProductModelTests
{
    [Fact]
    public void CreateScript_UsesBoundedSequenceAndProductConstraints()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=true;TrustServerCertificate=true")
            .Options;
        using var context = new ProductDbContext(options);

        var sql = context.Database.GenerateCreateScript();

        Assert.Contains("CREATE SEQUENCE [dbo].[ProductIds]", sql);
        Assert.Contains("START WITH 100005", sql);
        Assert.Contains("MINVALUE 100000 MAXVALUE 999999 NO CYCLE", sql);
        Assert.Contains("DEFAULT (NEXT VALUE FOR [dbo].[ProductIds])", sql);
        Assert.Contains("[Name] nvarchar(200) NOT NULL", sql);
        Assert.Contains("[Description] nvarchar(2000) NULL", sql);
        Assert.Contains("[Price] decimal(18,2) NOT NULL", sql);
        Assert.Contains("CHECK ([Id] BETWEEN 100000 AND 999999)", sql);
        Assert.Contains("CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)", sql);
        Assert.Contains("CHECK ([Price] > 0)", sql);
        Assert.Contains("CHECK ([Stock] >= 0)", sql);
    }
}
