﻿using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class LotService
    {
        private readonly LotRepository _lotRepository;
        private readonly ILogger<LotService> _logger;
        public LotService(LotRepository lotRepository, ILogger<LotService> logger)
        {
            _lotRepository = lotRepository;
            _logger = logger;
        }

        #region Get by lot id and product id

        public async Task<Lot> GetLotForProductAsync(string rawLotId, string rawProductId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var lotId = LotCodeHelper.FromPublicId(rawLotId);
            var productId = ProductCodeHelper.FromPublicId(rawProductId);

            var lot = await _lotRepository.GetForProductAsync(lotId, productId, cancellationToken)
                             ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { rawLotId, rawProductId });

            if (lot.Deleted && !includeDeleted)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { rawLotId, rawProductId });
            }

            return lot;
        }

        #endregion

        #region Update

        public async Task<Lot> UpdateLotAsync(string publicId, Lot request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var lotId = LotCodeHelper.FromPublicId(publicId);
            var lot = await _lotRepository.GetByIdAsync(lotId, cancellationToken)
                            ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

            if (lot.Deleted)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });
            }

            //await ValidateUpdateAsync(request, lot.LotId, cancellationToken);

            var updated = ApplyUpdates(lot, request);
            await _lotRepository.UpdateAsync(updated, cancellationToken);

            return updated;
        }

        //private async Task ValidateUpdateAsync(Lot request, long lotId, CancellationToken cancellationToken)
        //{

        //}

        private static Lot ApplyUpdates(Lot lot, Lot request)
        {
            var utcNow = DateTime.UtcNow;

            lot.ProductId = request.ProductId;

            if (!string.IsNullOrWhiteSpace(request.LotCode))
            {
                lot.LotCode = request.LotCode.Trim();
            }

            lot.ExpiryDate = request.ExpiryDate;

            lot.ReceivedAt = request.ReceivedAt;

            lot.QtyOnHand = request.QtyOnHand;

            lot.LastModifiedAt = utcNow;
            return lot;
        }

        #endregion

    }
}
