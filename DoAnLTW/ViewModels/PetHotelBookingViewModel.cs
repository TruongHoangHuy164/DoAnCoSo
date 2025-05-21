using DoAnLTW.Models;
using System.Collections.Generic;

namespace DoAnLTW.ViewModels
{
    public class PetHotelBookingViewModel
    {
        public List<PetHotelBooking> Bookings { get; set; }
        public List<HotelRoom> Rooms { get; set; }
    }
}