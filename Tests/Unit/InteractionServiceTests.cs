using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Modules.Catalog.Services.Interfaces;
using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Implementations;
using Application.Modules.Engagement.Services.Interfaces;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Catalog;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Domain.Entities.Interactions;
using Domain.Entities.Vendors;
using Moq;
using Xunit;

namespace FifeN.Tests.Unit
{
    public class InteractionServiceTests
    {
        [Fact]
        public async Task GetChatsAsync_ClampsPaginationAndLoadsCurrentUsersChats()
        {
            var userId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;
            var interactions = new Mock<IInteractionRepository>();
            interactions.Setup(repository => repository.GetChatsAsync(
                    userId, 1, 50, cancellationToken))
                .ReturnsAsync(new Application.DTOs.PagedResponse<ChatSummaryResponse>(
                    Array.Empty<ChatSummaryResponse>(), 1, 50, 0));

            var service = new InteractionService(
                interactions.Object, new Mock<IProductRepository>().Object,
                new Mock<IVendorRepository>().Object, new Mock<INotificationService>().Object);

            var response = await service.GetChatsAsync(userId, 0, 100, cancellationToken);

            Assert.Equal(1, response.Page);
            Assert.Equal(50, response.PageSize);
            interactions.Verify(repository => repository.GetChatsAsync(
                userId, 1, 50, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task ExpressInterestAsync_WithMessage_AddsBuyerMessageToChat()
        {
            var buyerUserId = Guid.NewGuid();
            var vendorUserId = Guid.NewGuid();
            var vendorProfileId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;
            var savedMessage = (ChatMessage?)null;

            var interactions = new Mock<IInteractionRepository>();
            interactions.Setup(repository => repository.GetByBuyerAndProductAsync(
                    buyerUserId, productId, cancellationToken))
                .ReturnsAsync((Interaction?)null);
            interactions.Setup(repository => repository.HasPriorWithVendorAsync(
                    buyerUserId, vendorProfileId, cancellationToken))
                .ReturnsAsync(false);
            interactions.Setup(repository => repository.AddAsync(
                    It.IsAny<Interaction>(), cancellationToken))
                .Returns(Task.CompletedTask);
            interactions.Setup(repository => repository.AddMessageAsync(
                    It.IsAny<ChatMessage>(), cancellationToken))
                .Callback<ChatMessage, CancellationToken>((message, _) => savedMessage = message)
                .Returns((ChatMessage message, CancellationToken _) => Task.FromResult(
                    new ChatMessageResponse(message.Id, message.InteractionId, message.SenderUserId,
                        "Buyer", message.Body, message.IsSystem, message.CreatedAtUtc)));

            var products = new Mock<IProductRepository>();
            products.Setup(repository => repository.GetDetailAsync(productId, cancellationToken))
                .ReturnsAsync(LiveProduct(productId, vendorProfileId, vendorUserId));

            var vendors = new Mock<IVendorRepository>();
            var notifications = new Mock<INotificationService>();
            notifications.Setup(service => service.NotifyAsync(
                    It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = new InteractionService(
                interactions.Object, products.Object, vendors.Object, notifications.Object);

            var response = await service.ExpressInterestAsync(
                buyerUserId, productId,
                new ExpressInterestRequest("  Hello, I'd like to discuss delivery.  ", null),
                cancellationToken);

            Assert.NotNull(response.Message);
            Assert.Equal(response.InteractionId, response.Message.InteractionId);
            Assert.Equal("/chats/" + response.InteractionId, response.ChatPath);
            Assert.NotNull(savedMessage);
            Assert.Equal(buyerUserId, savedMessage.SenderUserId);
            Assert.Equal("Hello, I'd like to discuss delivery.", savedMessage.Body);
            Assert.False(savedMessage.IsSystem);
        }

        [Fact]
        public async Task ExpressInterestAsync_WithUpdatedMessageOnExistingLead_AddsItToChat()
        {
            var buyerUserId = Guid.NewGuid();
            var vendorUserId = Guid.NewGuid();
            var vendorProfileId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;
            var lead = new Interaction
            {
                Id = Guid.NewGuid(),
                BuyerUserId = buyerUserId,
                VendorProfileId = vendorProfileId,
                ProductId = productId,
                BuyerMessage = "Previous note"
            };
            var savedMessage = (ChatMessage?)null;

            var interactions = new Mock<IInteractionRepository>();
            interactions.Setup(repository => repository.GetByBuyerAndProductAsync(
                    buyerUserId, productId, cancellationToken))
                .ReturnsAsync(lead);
            interactions.Setup(repository => repository.SaveAsync(lead, cancellationToken))
                .Returns(Task.CompletedTask);
            interactions.Setup(repository => repository.AddMessageAsync(
                    It.IsAny<ChatMessage>(), cancellationToken))
                .Callback<ChatMessage, CancellationToken>((message, _) => savedMessage = message)
                .Returns((ChatMessage message, CancellationToken _) => Task.FromResult(
                    new ChatMessageResponse(message.Id, message.InteractionId, message.SenderUserId,
                        "Buyer", message.Body, message.IsSystem, message.CreatedAtUtc)));

            var products = new Mock<IProductRepository>();
            products.Setup(repository => repository.GetDetailAsync(productId, cancellationToken))
                .ReturnsAsync(LiveProduct(productId, vendorProfileId, vendorUserId));

            var service = new InteractionService(
                interactions.Object, products.Object, new Mock<IVendorRepository>().Object,
                new Mock<INotificationService>().Object);

            var response = await service.ExpressInterestAsync(
                buyerUserId, productId, new ExpressInterestRequest("Updated note", null), cancellationToken);

            Assert.NotNull(response.Message);
            Assert.Equal(lead.Id, response.Message.InteractionId);
            Assert.Equal("Updated note", savedMessage?.Body);
            interactions.Verify(repository => repository.AddAsync(
                It.IsAny<Interaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static Product LiveProduct(Guid productId, Guid vendorProfileId, Guid vendorUserId) => new()
        {
            Id = productId,
            VendorProfileId = vendorProfileId,
            Title = "Egyptian Black Abaya",
            Vendor = new VendorProfile
            {
                Id = vendorProfileId,
                UserId = vendorUserId,
                User = new User { Id = vendorUserId, Status = UserStatus.Active }
            }
        };
    }
}