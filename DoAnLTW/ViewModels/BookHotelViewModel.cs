using DoAnLTW.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnLTW.Controllers.ViewModels
{
    public class BookHotelViewModel
    {
        public int RoomTypeId { get; set; }

        public int RoomId { get; set; }

        public int PetId { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(200)]
        public string Address { get; set; }

        public string Note { get; set; }

        public RoomType SelectedRoomType { get; set; }

        public List<Pet> UserPets { get; set; } = new List<Pet>();

        public List<HotelRoom> AvailableRooms { get; set; } = new List<HotelRoom>();

        public List<RoomType> RoomTypes { get; set; } = new List<RoomType>();
    }
}