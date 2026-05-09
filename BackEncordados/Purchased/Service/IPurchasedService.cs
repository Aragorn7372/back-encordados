using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Errors;
using BackEncordados.Purchased.Model;
using CSharpFunctionalExtensions;

namespace BackEncordados.Purchased.Service;

public interface IPurchasedService
{
    Task<PageResponseDto<PurchasedResponseDto>> FindAllAsync(FilterPurchasedDto filter);
    Task<Result<PurchasedResponseDto, DomainErrors>> FindByIdAsync(Ulid id);
    Task<Result<PurchasedResponseDto, DomainErrors>> CreatePurchasedAsync(PurchasedRequestDto request);
    Task<Result<PurchasedResponseDto, DomainErrors>> UpdatePurchasedAsync(Ulid id, PurchasedPatchDto request);
    Task<Result<PurchasedResponseDto, DomainErrors>> CancelPurchasedAsync(Ulid id, bool isUser, string? idUser);
    Task<Result<PurchasedResponseDto, DomainErrors>> ChangeStatusPurchasedAsync(Ulid id, string status);
    Task<Result<PurchasedResponseDto, DomainErrors>> ChangePaymentStatusPurchasedAsync(Ulid id, string payStatus);
}