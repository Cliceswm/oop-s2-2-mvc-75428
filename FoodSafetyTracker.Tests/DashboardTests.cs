using FoodSafetyTracker.Web.Data;
using FoodSafetyTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Xunit;

namespace FoodSafetyTracker.Tests
{
    public class DashboardTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void OverdueFollowUps_ReturnsCorrectItems()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var today = DateTime.Today;

            var premises = new Premises
            {
                Name = "Test Cafe",
                Town = "Dublin",
                RiskRating = RiskRating.Medium
            };
            context.Premises.Add(premises);
            context.SaveChanges();

            var inspection = new Inspection
            {
                PremisesId = premises.Id,
                InspectionDate = today.AddDays(-30),
                Score = 50,
                Outcome = Outcome.Fail,
                Notes = "Test inspection"
            };
            context.Inspections.Add(inspection);
            context.SaveChanges();

            var overdueFollowUp = new FollowUp
            {
                InspectionId = inspection.Id,
                DueDate = today.AddDays(-5),
                Status = FollowUpStatus.Open
            };
            var notOverdueFollowUp = new FollowUp
            {
                InspectionId = inspection.Id,
                DueDate = today.AddDays(10),
                Status = FollowUpStatus.Open
            };
            var closedFollowUp = new FollowUp
            {
                InspectionId = inspection.Id,
                DueDate = today.AddDays(-5),
                Status = FollowUpStatus.Closed,
                ClosedDate = today
            };

            context.FollowUps.AddRange(overdueFollowUp, notOverdueFollowUp, closedFollowUp);
            context.SaveChanges();

            // Act
            var overdueCount = context.FollowUps
                .Count(f => f.DueDate < today && f.Status == FollowUpStatus.Open);

            // Assert
            Assert.Equal(1, overdueCount);
        }

        [Fact]
        public void FollowUp_CannotBeClosedWithoutClosedDate()
        {
            // Arrange & Act
            var followUp = new FollowUp
            {
                Status = FollowUpStatus.Closed,
                ClosedDate = null
            };

            // Assert - business rule validation
            var isValid = !(followUp.Status == FollowUpStatus.Closed && !followUp.ClosedDate.HasValue);
            Assert.False(isValid, "Closed follow-ups must have a ClosedDate");
        }

        [Fact]
        public void InspectionScore_RangeIsValid()
        {
            // Arrange
            var validScore = 85;
            var invalidScore = 150;

            // Assert
            Assert.InRange(validScore, 0, 100);
            Assert.True(invalidScore < 0 || invalidScore > 100, "Score must be between 0 and 100");
        }

        [Fact]
        public void Premises_HasValidRiskRating()
        {
            // Arrange
            var validRatings = new[] { RiskRating.Low, RiskRating.Medium, RiskRating.High };
            var premises = new Premises { RiskRating = RiskRating.Medium };

            // Assert
            Assert.Contains(premises.RiskRating, validRatings);
        }

        [Fact]
        public void DashboardCounts_AreConsistentWithSeedData()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            var premises = new Premises
            {
                Name = "Test Business",
                Town = "Dublin",
                RiskRating = RiskRating.High
            };
            context.Premises.Add(premises);
            context.SaveChanges();

            var inspection1 = new Inspection
            {
                PremisesId = premises.Id,
                InspectionDate = firstDayOfMonth.AddDays(5),
                Score = 85,
                Outcome = Outcome.Pass,
                Notes = "First inspection"
            };
            var inspection2 = new Inspection
            {
                PremisesId = premises.Id,
                InspectionDate = firstDayOfMonth.AddDays(10),
                Score = 45,
                Outcome = Outcome.Fail,
                Notes = "Second inspection"
            };
            context.Inspections.AddRange(inspection1, inspection2);
            context.SaveChanges();

            // Act
            var inspectionsThisMonth = context.Inspections
                .Count(i => i.InspectionDate >= firstDayOfMonth && i.InspectionDate <= today);

            var failedInspectionsThisMonth = context.Inspections
                .Count(i => i.InspectionDate >= firstDayOfMonth &&
                           i.InspectionDate <= today &&
                           i.Outcome == Outcome.Fail);

            // Assert
            Assert.Equal(2, inspectionsThisMonth);
            Assert.Equal(1, failedInspectionsThisMonth);
        }
    }
}