using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Application.DTO.Finance
{
    public class UpdateFavoriteCurrenciesDto
    {
        public List<Guid> CurrencyIds { get; set; }

        public Guid UserId { get; set; }
    }
}
