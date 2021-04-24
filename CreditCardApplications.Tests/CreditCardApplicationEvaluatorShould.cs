using System;
using Xunit;
using Moq;
using Moq.Protected;
using System.Collections.Generic;

namespace CreditCardApplications.Tests
{
    public class CreditCardApplicationEvaluatorShould
    {
        private Mock<IFrequentFlyerNumberValidator> mockValidator;
        private CreditCardApplicationEvaluator sut;

        public CreditCardApplicationEvaluatorShould()
        {
            mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.SetupAllProperties();
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true);

            sut = new CreditCardApplicationEvaluator(mockValidator.Object);
        }

        [Fact]
        public void AcceptHighIncomeApplications()
        {
            var application = new CreditCardApplication { GrossAnnualIncome = 100_000 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.AutoAccepted, decision);
        }

        [Fact]
        public void ReferYoungApplications()
        {
            mockValidator.DefaultValue = DefaultValue.Mock;

            var application = new CreditCardApplication { Age = 19 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        [Fact]
        public void DeclineLowIncomeApplications()
        {
            //mockValidator.Setup(x => x.IsValid("x")).Returns(true);
            //mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true);
            //mockValidator.Setup(
            //        x => x.IsValid(It.Is<string>(number => number.StartsWith("y"))))
            //    .Returns(true);
            //mockValidator.Setup(
            //        x => x.IsValid(It.IsInRange<string>("a", "z", Moq.Range.Inclusive)))
            //    .Returns(true);
            //mockValidator.Setup(
            //        x => x.IsValid(It.IsIn<string>("a", "z", "y")))
            //    .Returns(true);
            mockValidator.Setup(
                    x => x.IsValid(It.IsRegex("[a-z]")))
                .Returns(true);

            var application = new CreditCardApplication 
            { 
                GrossAnnualIncome = 19_999,
                Age = 42,
                FrequentFlyerNumber = "y"
            };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);
        }

        [Fact]
        public void ReferInvalidFrequentFlyerApplications()
        {
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(false);

            var application = new CreditCardApplication();

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        [Fact]
        public void DeclineLowIncomeApplicationsOutDemo()
        {
            bool isValid = true;
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>(), out isValid));

            var application = new CreditCardApplication()
            {
                GrossAnnualIncome = 19_999,
                Age = 42
            };

            CreditCardApplicationDecision decision = sut.EvaluateUsingOut(application);

            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);
        }

        [Fact]
        public void ReferWhenLicenseKeyExpired()
        {
            //var mockLicenseData = new Mock<ILicenseData>();
            //mockLicenseData.Setup(x => x.LicenseKey).Returns("EXPIRED");
            //var mockServiceInfo = new Mock<IServiceInformation>();
            //mockServiceInfo.Setup(x => x.License).Returns(mockLicenseData.Object);
            //mockValidator.Setup(x => x.ServiceInformation).Returns(mockServiceInfo.Object);
            mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("EXPIRED");

            var application = new CreditCardApplication { Age = 42 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        string GetLicenseKeyExpiredString()
        {
            // E.g. read from vendor-supplied constants file
            return "EXPIRED";
        }

        [Fact]
        public void UseDetailedLookupForOlderApplication()
        {
            var application = new CreditCardApplication { Age = 30 };

            sut.Evaluate(application);

            Assert.Equal(ValidationMode.Detailed, mockValidator.Object.ValidationMode);
        }

        [Fact]
        public void ValidateFrequentFlyerNumberForLowIncomeApplication()
        {
            var application = new CreditCardApplication
            {
                FrequentFlyerNumber = "q"
            };

            sut.Evaluate(application);

            //mockValidator.Verify(x => x.IsValid(It.IsAny<string>()), "Frequentflynumer should be validated");
            mockValidator.Verify(x => x.IsValid(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void NotValidateFrequentFlyerNumberForHighIncomeApplication()
        {
            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 100_000
            };

            sut.Evaluate(application);

            mockValidator.Verify(x => x.IsValid(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void CheckLicenseKeyForLowIncomeApplications()
        {
            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 99_000
            };

            sut.Evaluate(application);

            mockValidator.VerifyGet(x => x.ServiceInformation.License.LicenseKey);
        }

        [Fact]
        public void SetDetailedLookupForOlderApplication()
        {
            var application = new CreditCardApplication
            {
                Age = 30
            };

            sut.Evaluate(application);

            mockValidator.VerifySet(x => x.ValidationMode = ValidationMode.Detailed);

            //mockValidator.VerifyNoOtherCalls();
        }

        [Fact]
        public void ReferWhenFrequentFlyerValidationError()
        {
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Throws<Exception>();

            var application = new CreditCardApplication
            {
                Age = 42
            };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        [Fact]
        public void IncrementLookupCount()
        {
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>()))
                .Returns(true)
                .Raises(x => x.ValidatorLookupPerformed += null, EventArgs.Empty); // auto raise

            var application = new CreditCardApplication
            {
                FrequentFlyerNumber = "x",
                Age = 25
            };

            sut.Evaluate(application);

            //mockValidator.Raise(x => x.ValidatorLookupPerformed += null, EventArgs.Empty);  // manual raise

            Assert.Equal(1, sut.ValidatorLookupCount);
        }

        [Fact]
        public void ReferInvalidFrequentFlyerApplication_ReturnValuesSequence()
        {
            mockValidator.SetupSequence(x => x.IsValid(It.IsAny<string>()))
                .Returns(false)
                .Returns(true);

            var application = new CreditCardApplication
            {
                Age = 25
            };

            CreditCardApplicationDecision firstDecision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, firstDecision);

            CreditCardApplicationDecision secondDecision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, secondDecision);
        }

        [Fact]
        public void ReferInvalidFrequentFlyerApplication_MultipleCallsSequence()
        {
            var frequentFlyerNumbersPassed = new List<string>();
            mockValidator.Setup(x => x.IsValid(Capture.In(frequentFlyerNumbersPassed)));

            var application1 = new CreditCardApplication { Age = 25, FrequentFlyerNumber = "aa" };
            var application2 = new CreditCardApplication { Age = 25, FrequentFlyerNumber = "bb" };
            var application3 = new CreditCardApplication { Age = 25, FrequentFlyerNumber = "cc" };

            sut.Evaluate(application1);
            sut.Evaluate(application2);
            sut.Evaluate(application3);

            // Assert that IsValid was called three times with "aa", "bb", and "cc"
            Assert.Equal(new List<string> { "aa", "bb", "cc" }, frequentFlyerNumbersPassed);
        }

        [Fact]
        public void ReferFraudRisk()
        {
            Mock<FraudLookup> mockFraudLookup = new Mock<FraudLookup>();
            //mockFraudLookup.Setup(x => x.IsFraudRisk(It.IsAny<CreditCardApplication>()))
            //    .Returns(true);   // this no only work since not virtual any more
            mockFraudLookup.Protected()
                .Setup<bool>("CheckApplication", ItExpr.IsAny<CreditCardApplication>())
                .Returns(true);

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object, mockFraudLookup.Object);

            var application = new CreditCardApplication ();

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHumanFraudRisk, decision);
        }

        [Fact]
        public void LinqToMocks()
        {
            //Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            //mockValidator.Setup(x => x.ServiceInformation.License.LicenseKey).Returns("OK");
            //mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true);

            IFrequentFlyerNumberValidator mockValidator = Mock.Of<IFrequentFlyerNumberValidator>
                (
                    validator =>
                    validator.ServiceInformation.License.LicenseKey == "OK" &&
                    validator.IsValid(It.IsAny<string>()) == true
                );

            var sut = new CreditCardApplicationEvaluator(mockValidator);

            var application = new CreditCardApplication { Age = 25 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);
        }
    }
}
