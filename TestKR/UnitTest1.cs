using System;
using WpfLibrary1;
using Xunit;

namespace TestKR
{
    public class UnitTest1
    {
        [Fact]
        public void ComputeHash_IsDeterministic()
        {
            var a = AuthHelper.ComputeHash("password");
            var b = AuthHelper.ComputeHash("password");
            Assert.Equal(a, b);
        }

        [Fact]
        public void VerifyPassword_ReturnsTrueForCorrectPassword()
        {
            var pw = "s3cr3t";
            var h = AuthHelper.ComputeHash(pw);
            Assert.True(AuthHelper.VerifyPassword(pw, h));
        }

        [Fact]
        public void VerifyPassword_ReturnsFalseForWrongPassword()
        {
            var pw = "s3cr3t";
            var h = AuthHelper.ComputeHash(pw);
            Assert.False(AuthHelper.VerifyPassword("wrong", h));
        }

        [Fact]
        public void Officer_FullName_WithoutPatronymic()
        {
            var o = new Officer { FirstName = "Ivan", LastName = "Ivanov", Patronymic = null };
            Assert.Equal("Ivanov Ivan", o.FullName);
        }

        [Fact]
        public void Officer_FullName_WithPatronymic()
        {
            var o = new Officer { FirstName = "Ivan", LastName = "Ivanov", Patronymic = "Petrovich" };
            Assert.Equal("Ivanov Ivan Petrovich", o.FullName);
        }

        [Fact]
        public void Department_Address_ComposesCorrectly()
        {
            var d = new Department { Street = "Main", House = "1A", City = "Cityville" };
            Assert.Equal("Main, 1A, Cityville", d.Address);
        }

        [Fact]
        public void CaseRecord_Status_EnumMapping()
        {
            var c = new CaseRecord { Status = CaseStatus.InProgress };
            Assert.Equal((int)CaseStatus.InProgress, c.StatusId);
            c.Status = CaseStatus.Closed;
            Assert.Equal((int)CaseStatus.Closed, c.StatusId);
        }
    }
}