using System.Reflection;
using Domain.Entities.Identity;
using Application.Modules.Identity.Services.Implementations;
using NetArchTest.Rules;
using Persistence;
using Xunit;

namespace FifeN.Tests.Architecture
{
    /// <summary>
    /// Enforces the clean-architecture dependency direction documented in CLAUDE.md:
    /// <c>API → Application → Domain</c> and <c>Persistence → Domain</c>. Domain is the stable core
    /// and must not reference any outer layer; Application must not reach into Persistence or the API.
    /// </summary>
    public class LayerDependencyTests
    {
        private static readonly Assembly DomainAssembly = typeof(User).Assembly;
        private static readonly Assembly ApplicationAssembly = typeof(AuthenticationService).Assembly;
        private static readonly Assembly PersistenceAssembly = typeof(FifeNDbContext).Assembly;

        private const string DomainNs = "Domain";
        private const string ApplicationNs = "Application";
        private const string PersistenceNs = "Persistence";
        private const string ApiNs = "API";

        [Fact]
        public void Domain_DependingOnOuterLayers_HasNoViolations()
        {
            var result = Types.InAssembly(DomainAssembly)
                .ShouldNot()
                .HaveDependencyOnAny(ApplicationNs, PersistenceNs, ApiNs)
                .GetResult();

            Assert.True(result.IsSuccessful, Describe(result));
        }

        [Fact]
        public void Application_DependingOnPersistenceOrApi_HasNoViolations()
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .ShouldNot()
                .HaveDependencyOnAny(PersistenceNs, ApiNs)
                .GetResult();

            Assert.True(result.IsSuccessful, Describe(result));
        }

        [Fact]
        public void Persistence_DependingOnApi_HasNoViolations()
        {
            var result = Types.InAssembly(PersistenceAssembly)
                .ShouldNot()
                .HaveDependencyOn(ApiNs)
                .GetResult();

            Assert.True(result.IsSuccessful, Describe(result));
        }

        [Fact]
        public void Domain_Entities_DoNotDependOnEntityFrameworkCore()
        {
            // Persistence concerns (EF Core) stay out of the domain model; mapping lives in Persistence.
            var result = Types.InAssembly(DomainAssembly)
                .That()
                .ResideInNamespaceStartingWith("Domain.Entities")
                .ShouldNot()
                .HaveDependencyOn("Microsoft.EntityFrameworkCore")
                .GetResult();

            Assert.True(result.IsSuccessful, Describe(result));
        }

        private static string Describe(TestResult result) =>
            result.IsSuccessful
                ? "No violations."
                : "Offending types:\n" + string.Join("\n", result.FailingTypeNames);
    }
}
