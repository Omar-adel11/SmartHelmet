using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs;

namespace BLL.Abstractions
{
    public interface IRideService
    {
        Task AddRideAsync(RideDTO rideDto, int userId);
        Task<IEnumerable<RideDTO>> GetUserRidesAsync(int userId);
        Task<IEnumerable<WeeklyVitalsDTO>> GetWeeklyVitalsAsync(int userId);
    }
}
