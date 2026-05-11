using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Notifications;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(
        IRepository<Notification> notifications,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var query = _notifications.Query().Where(n => n.UserId == userId);

        query = parameters.SortDescending ? query.OrderByDescending(n => n.CreatedDate) : query.OrderBy(n => n.CreatedDate);

        var page = await query.ProjectTo<NotificationDto>(_mapper.ConfigurationProvider)
            .ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        return page;
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        return await _notifications.Query().CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var n = await _notifications.Query().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (n == null) return;

        n.IsRead = true;
        _notifications.Update(n);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
