// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace IdentityBase.EntityFramework.IntegrationTests.Stores
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IdentityBase.EntityFramework.DbContexts;
    using IdentityBase.EntityFramework.Mappers;
    using IdentityBase.EntityFramework.Configuration;
    using IdentityBase.EntityFramework.Stores;
    using IdentityServer4.Models;
    using Microsoft.EntityFrameworkCore;
    using ServiceBase.Logging;
    using Xunit;

    public class PersistedGrantStoreTests :
        IClassFixture<DatabaseProviderFixture<PersistedGrantDbContext>>
    {
        private static readonly EntityFrameworkOptions StoreOptions =
            new EntityFrameworkOptions();

        public static readonly TheoryData<DbContextOptions<PersistedGrantDbContext>>
            TestDatabaseProviders = new TheoryData<DbContextOptions<PersistedGrantDbContext>>
        {
            DatabaseProviderBuilder.BuildInMemory<PersistedGrantDbContext>(
                nameof(PersistedGrantStoreTests), StoreOptions),

            DatabaseProviderBuilder.BuildSqlite<PersistedGrantDbContext>(
                nameof(PersistedGrantStoreTests), StoreOptions),

            DatabaseProviderBuilder.BuildSqlServer<PersistedGrantDbContext>(
                nameof(PersistedGrantStoreTests), StoreOptions)
        };

        public PersistedGrantStoreTests(
            DatabaseProviderFixture<PersistedGrantDbContext> fixture)
        {
            fixture.Options = TestDatabaseProviders
                .SelectMany(x => x
                    .Select(y => (DbContextOptions<PersistedGrantDbContext>)y))
                .ToList();
            
            fixture.StoreOptions = StoreOptions;
        }

        private static PersistedGrant CreateTestObject()
        {
            return new PersistedGrant
            {
                Key = Guid.NewGuid().ToString(),
                Type = "authorization_code",
                ClientId = Guid.NewGuid().ToString(),
                SubjectId = Guid.NewGuid().ToString(),
                CreationTime = new DateTime(2016, 08, 01),
                Expiration = new DateTime(2016, 08, 31),
                Data = Guid.NewGuid().ToString()
            };
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void StoreAsync_WhenPersistedGrantStored_ExpectSuccess(
            DbContextOptions<PersistedGrantDbContext> options)
        {
            var persistedGrant = CreateTestObject();

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var store = new PersistedGrantStore(
                    context,
                    NullLogger<PersistedGrantStore>.Create()
                );

                store.StoreAsync(persistedGrant).Wait();
            }

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var foundGrant = context.PersistedGrants
                    .FirstOrDefault(x => x.Key == persistedGrant.Key);

                Assert.NotNull(foundGrant);
            }
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void GetAsync_WithKeyAndPersistedGrantExists_ExpectPersistedGrantReturned(
            DbContextOptions<PersistedGrantDbContext> options)
        {
            var persistedGrant = CreateTestObject();

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                context.PersistedGrants.Add(persistedGrant.ToEntity());
                context.SaveChanges();
            }

            PersistedGrant foundPersistedGrant;
            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var store = new PersistedGrantStore(
                    context,
                    NullLogger<PersistedGrantStore>.Create()
                );

                foundPersistedGrant = store.GetAsync(persistedGrant.Key).Result;
            }

            Assert.NotNull(foundPersistedGrant);
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void GetAsync_WithSubAndTypeAndPersistedGrantExists_ExpectPersistedGrantReturned(
            DbContextOptions<PersistedGrantDbContext> options)
        {
            var persistedGrant = CreateTestObject();

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                context.PersistedGrants.Add(persistedGrant.ToEntity());
                context.SaveChanges();
            }

            IList<PersistedGrant> foundPersistedGrants;
            using (var context = new PersistedGrantDbContext(options, StoreOptions))
            {
                var store = new PersistedGrantStore(
                    context,
                    NullLogger<PersistedGrantStore>.Create()
                );

                foundPersistedGrants = store
                    .GetAllAsync(persistedGrant.SubjectId).Result
                    .ToList();
            }

            Assert.NotNull(foundPersistedGrants);
            Assert.NotEmpty(foundPersistedGrants);
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void RemoveAsync_WhenKeyOfExistingReceived_ExpectGrantDeleted(
            DbContextOptions<PersistedGrantDbContext> options)
        {
            var persistedGrant = CreateTestObject();

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                context.PersistedGrants.Add(persistedGrant.ToEntity());
                context.SaveChanges();
            }
            
            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var store = new PersistedGrantStore(
                    context,
                    NullLogger<PersistedGrantStore>.Create()
                );

                store.RemoveAsync(persistedGrant.Key).Wait();
            }

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var foundGrant = context.PersistedGrants
                    .FirstOrDefault(x => x.Key == persistedGrant.Key);

                Assert.Null(foundGrant);
            }
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void RemoveAsync_WhenSubIdAndClientIdOfExistingReceived_ExpectGrantDeleted(
            DbContextOptions<PersistedGrantDbContext> options)
        {
            var persistedGrant = CreateTestObject();

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                context.PersistedGrants.Add(persistedGrant.ToEntity());
                context.SaveChanges();
            }

            using (var context = new PersistedGrantDbContext(options, StoreOptions))
            {
                var store = new PersistedGrantStore(
                    context,
                    NullLogger<PersistedGrantStore>.Create()
                );

                store.RemoveAllAsync(
                    persistedGrant.SubjectId,
                    persistedGrant.ClientId
                ).Wait();
            }

            using (var context = new PersistedGrantDbContext(options, StoreOptions))
            {
                var foundGrant = context.PersistedGrants
                    .FirstOrDefault(x => x.Key == persistedGrant.Key);

                Assert.Null(foundGrant);
            }
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void RemoveAsync_WhenSubIdClientIdAndTypeOfExistingReceived_ExpectGrantDeleted(
            DbContextOptions<PersistedGrantDbContext> options)
        {
            var persistedGrant = CreateTestObject();

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                context.PersistedGrants.Add(persistedGrant.ToEntity());
                context.SaveChanges();
            }

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var store = new PersistedGrantStore(
                    context,
                    NullLogger<PersistedGrantStore>.Create()
                );

                store.RemoveAllAsync(
                    persistedGrant.SubjectId,
                    persistedGrant.ClientId,
                    persistedGrant.Type
                ).Wait();
            }

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var foundGrant = context.PersistedGrants
                    .FirstOrDefault(x => x.Key == persistedGrant.Key);

                Assert.Null(foundGrant);
            }
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void Store_should_create_new_record_if_key_does_not_exist(
            DbContextOptions<PersistedGrantDbContext> options)
        {
            var persistedGrant = CreateTestObject();

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var foundGrant = context.PersistedGrants
                    .FirstOrDefault(x => x.Key == persistedGrant.Key);

                Assert.Null(foundGrant);
            }

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var store = new PersistedGrantStore(
                    context,
                    NullLogger<PersistedGrantStore>.Create()
                );

                store.StoreAsync(persistedGrant).Wait();
            }

            using (var context = new PersistedGrantDbContext(options, StoreOptions))
            {
                var foundGrant = context.PersistedGrants
                    .FirstOrDefault(x => x.Key == persistedGrant.Key);

                Assert.NotNull(foundGrant);
            }
        }

        [Theory, MemberData(nameof(TestDatabaseProviders))]
        public void Store_should_update_record_if_key_already_exists(
            DbContextOptions<PersistedGrantDbContext> options)
        {
            var persistedGrant = CreateTestObject();

            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                context.PersistedGrants.Add(persistedGrant.ToEntity());
                context.SaveChanges();
            }

            var newDate = persistedGrant.Expiration.Value.AddHours(1);
            using (var context =
                new PersistedGrantDbContext(options, StoreOptions))
            {
                var store = new PersistedGrantStore(
                    context,
                    NullLogger<PersistedGrantStore>.Create()
                );

                persistedGrant.Expiration = newDate;
                store.StoreAsync(persistedGrant).Wait();
            }

            using (var context = new PersistedGrantDbContext(options, StoreOptions))
            {
                var foundGrant = context.PersistedGrants
                    .FirstOrDefault(x => x.Key == persistedGrant.Key);

                Assert.NotNull(foundGrant);
                Assert.Equal(newDate, persistedGrant.Expiration);
            }
        }
    }
}