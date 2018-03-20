// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace IdentityBase.EntityFramework.UnitTests.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using IdentityBase.EntityFramework.Mappers;
    using Xunit;
    using Client = IdentityServer4.Models.Client;

    public class ClientMappersTests
    {
       // [Fact]
       // public void ClientAutomapperConfigurationIsValid()
       // {
       //     ClientMappers.Mapper.ConfigurationProvider
       //         .AssertConfigurationIsValid();
       //
       //     var model = new Client();
       //     var mappedEntity = model.ToEntity();
       //     var mappedModel = mappedEntity.ToModel();
       //
       //     Assert.NotNull(mappedModel);
       //     Assert.NotNull(mappedEntity);
       // }

        [Fact]
        public void Properties_Map()
        {
            var model = new Client()
            {
                Properties =
                {
                    {"foo1", "bar1"},
                    {"foo2", "bar2"},
                }
            };
            
            var mappedEntity = model.ToEntity();

            mappedEntity.Properties.Count.Should().Be(2);

            var foo1 = mappedEntity.Properties
                .FirstOrDefault(x => x.Key == "foo1");

            foo1.Should().NotBeNull();
            foo1.Value.Should().Be("bar1");

            var foo2 = mappedEntity.Properties
                .FirstOrDefault(x => x.Key == "foo2");

            foo2.Should().NotBeNull();
            foo2.Value.Should().Be("bar2");

            var mappedModel = mappedEntity.ToModel();

            mappedModel.Properties.Count.Should().Be(2);
            mappedModel.Properties.ContainsKey("foo1").Should().BeTrue();
            mappedModel.Properties.ContainsKey("foo2").Should().BeTrue();
            mappedModel.Properties["foo1"].Should().Be("bar1");
            mappedModel.Properties["foo2"].Should().Be("bar2");
        }

        [Fact]
        public void Duplicates_properties_in_db_map()
        {
            var entity = new Entities.Client
            {
                Properties = new List<Entities.ClientProperty>()
                {
                    new Entities.ClientProperty{Key = "foo1", Value = "bar1"},
                    new Entities.ClientProperty{Key = "foo1", Value = "bar2"},
                }
            };

            Action modelAction = () => entity.ToModel();
            modelAction.ShouldThrow<Exception>();
        }
    }
}