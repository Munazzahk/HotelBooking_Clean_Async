using System;
using HotelBooking.Core;
using HotelBooking.UnitTests.Fakes;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;



namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private IBookingManager bookingManager;
        IRepository<Booking> bookingRepository;

        public BookingManagerTests()
        {
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(20);
            bookingRepository = new FakeBookingRepository(start, end);
            IRepository<Room> roomRepository = new FakeRoomRepository();
            bookingManager = new BookingManager(bookingRepository, roomRepository);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange
            DateTime date = DateTime.Today;

            // Act
            Task result() => bookingManager.FindAvailableRoom(date, date);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(result);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);
            // Assert
            Assert.NotEqual(-1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
        {
            // This test was added to satisfy the following test design
            // principle: "Tests should have strong assertions".

            // Arrange
            DateTime date = DateTime.Today.AddDays(1);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);

            var bookingForReturnedRoomId = (await bookingRepository.GetAllAsync()).
                Where(b => b.RoomId == roomId
                           && b.StartDate <= date
                           && b.EndDate >= date
                           && b.IsActive);

            // Assert
            Assert.Empty(bookingForReturnedRoomId);
        }


        [Fact]
        public async Task FindAvailableRoom_StartDateAfterEndDate_ThrowsException()
        {
            DateTime start = DateTime.Today.AddDays(5);
            DateTime end = DateTime.Today.AddDays(2);

            Task result() => bookingManager.FindAvailableRoom(start, end);

            await Assert.ThrowsAsync<ArgumentException>(result);
        }

        //Test for valid future dates
        [Theory]
        [InlineData(1, 5)]
        [InlineData(2, 4)]
        [InlineData(3, 3)] //Single day booking
        public async Task FindAvailableRoom_ValidFutureDates_ReturnsRoomId(int startOffset, int endOffset)
        {
            // Arrange
            DateTime start = DateTime.Today.AddDays(startOffset);
            DateTime end = DateTime.Today.AddDays(endOffset);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(start, end);

            // Assert
            Assert.NotEqual(-1, roomId);
        }


        [Fact]
        public async Task CreateBooking_RoomAvailable_ReturnsTrue()
        {
            // Arrange
            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            // Act
            var result = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(result);
            Assert.True(booking.IsActive);
        }

        //Test for invalid date range in CreateBooking (start date after end date)
        [Fact]
        public async Task GetFullyOccupiedDates_StartAfterEnd_ThrowsArgumentException()
        {
            // Arrange
            DateTime start = DateTime.Today.AddDays(5);
            DateTime end = DateTime.Today.AddDays(1);

            // Act
            Task result() => bookingManager.GetFullyOccupiedDates(start, end);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(result);
        }


        //Mocking test to verify that method itself, AddAsync is called when creating booking - ensure the booking saved to repository
        [Fact]
        public async Task CreateBooking_WithMoq_CallsAddAsync()
        {
            // Arrange
            var mockBookingRepo = new Mock<IRepository<Booking>>();
            var mockRoomRepo = new Mock<IRepository<Room>>();

            // Only 1 room exists to verify booking creation without conflicts
            mockRoomRepo.Setup(r => r.GetAllAsync())
                        .ReturnsAsync(new List<Room> { new Room { Id = 1 } });

            // No existing bookings to ensure room is available
            mockBookingRepo.Setup(b => b.GetAllAsync())
                           .ReturnsAsync(new List<Booking>());

            var manager = new BookingManager(mockBookingRepo.Object, mockRoomRepo.Object);

            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            // Act
            await manager.CreateBooking(booking);

            // Assert: verify AddAsync is called once
            mockBookingRepo.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Once);
        }
    }
}
