using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Wallet.Api.Controllers;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;
using Wallet.Api.Infrastructure;
using Wallet.Api.Services;

namespace Wallet.Tests.Unit;

/// <summary>
/// The controller is tested in isolation: the service is a mock, so these tests only
/// check the HTTP concerns (status codes, Location header, which user id is passed on).
/// </summary>
public class AccountsControllerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IAccountService> _accountService = new();

    private AccountsController CreateController()
    {
        var identity = new ClaimsIdentity([new Claim(AppClaims.Subject, _userId.ToString())], "Test");

        return new AccountsController(_accountService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    [Fact]
    public async Task Create_returns_201_pointing_at_the_new_account()
    {
        var request = new CreateAccountRequest("Savings", Currency.TRY);
        var created = new AccountResponse(Guid.NewGuid(), "Savings", Currency.TRY, 0m, DateTimeOffset.UtcNow);
        _accountService
            .Setup(s => s.CreateAsync(_userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await CreateController().Create(request, CancellationToken.None);

        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(AccountsController.GetById), createdAt.ActionName);
        Assert.Equal(created.Id, createdAt.RouteValues!["id"]);
        Assert.Same(created, createdAt.Value);
    }

    [Fact]
    public async Task GetById_asks_the_service_for_the_account_of_the_authenticated_user()
    {
        var account = new AccountResponse(Guid.NewGuid(), "Main", Currency.USD, 42m, DateTimeOffset.UtcNow);
        _accountService
            .Setup(s => s.GetByIdAsync(_userId, account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await CreateController().GetById(account.Id, CancellationToken.None);

        Assert.Same(account, result.Value);
        _accountService.Verify(s => s.GetByIdAsync(_userId, account.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Request_without_a_subject_claim_is_rejected()
    {
        var controller = new AccountsController(_accountService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetAll(CancellationToken.None));
    }
}
