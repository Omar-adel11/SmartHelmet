using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Abstractions;
using DAL.Users;
using Shared.DTOs;

namespace BLL.Services.Rides
{
    public class RideService(IUnitOfWork _unitOfWork) : IRideService
    {
        public async Task AddRideAsync(RideDTO rideDto, int userId)
        {
            var ride = new Ride
            {
                AppUserId = userId,
                RideDate = DateTime.UtcNow,
                Duration = rideDto.Duration,
                AvgHeartRate = rideDto.AvgHeartRate
            };
            await _unitOfWork.GetRepository<Ride, int>().AddAsync(ride);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<RideDTO>> GetUserRidesAsync(int userId)
        {
            var rides = await _unitOfWork.GetRepository<Ride, int>().GetAllAsync();
            return rides.Where(r => r.AppUserId == userId)
                        .OrderByDescending(r => r.RideDate)
                        .Select(r => new RideDTO
                        {
                            Id = r.Id,
                            RideDate = r.RideDate,
                            Duration = r.Duration,
                            AvgHeartRate = r.AvgHeartRate
                        });
        }

        public async Task<IEnumerable<WeeklyVitalsDTO>> GetWeeklyVitalsAsync(int userId)
        {
            var lastWeek = DateTime.UtcNow.AddDays(-7);
            var rides = await _unitOfWork.GetRepository<Ride, int>().GetAllAsync();

            return rides.Where(r => r.AppUserId == userId && r.RideDate >= lastWeek)
                        .GroupBy(r => r.RideDate.DayOfWeek)
                        .Select(g => new WeeklyVitalsDTO
                        {
                            Day = g.Key.ToString().Substring(0, 3),
                            AvgHeartRate = (int)g.Average(r => r.AvgHeartRate)
                        }).ToList();
        }
    }
}
