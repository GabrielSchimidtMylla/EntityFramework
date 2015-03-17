// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Data.Entity.Internal
{
    public class DbContextServices : IDbContextServices
    {

        private IServiceProvider _provider;
        private IDbContextOptions _contextOptions;
        private DbContext _context;
        private LazyRef<IModel> _modelFromSource;
        private LazyRef<IDataStoreServices> _dataStoreServices;
        private bool _inOnModelCreating;

        public virtual IDbContextServices Initialize(
            IServiceProvider scopedProvider, 
            IDbContextOptions contextOptions, 
            DbContext context,
            ServiceProviderSource serviceProviderSource)
        {
            Check.NotNull(scopedProvider, nameof(scopedProvider));
            Check.NotNull(contextOptions, nameof(contextOptions));
            Check.NotNull(context, nameof(context));
            Check.IsDefined(serviceProviderSource, nameof(serviceProviderSource));

            _provider = scopedProvider;
            _contextOptions = contextOptions;
            _context = context;

            _dataStoreServices = new LazyRef<IDataStoreServices>(() =>
                _provider.GetRequiredService<IDataStoreSelector>().SelectDataStore(serviceProviderSource));

            _modelFromSource = new LazyRef<IModel>(CreateModel);

            return this;
        }

        private IModel CreateModel()
        {
            if (_inOnModelCreating)
            {
                throw new InvalidOperationException(Strings.RecursiveOnModelCreating);
            }

            try
            {
                _inOnModelCreating = true;
                return _dataStoreServices.Value.ModelSource.GetModel(_context, _dataStoreServices.Value.ModelBuilderFactory);
            }
            finally
            {
                _inOnModelCreating = false;
            }
        }

        public virtual DbContext Context => _context;

        public virtual IModel Model => _contextOptions.FindExtension<CoreOptionsExtension>()?.Model ?? _modelFromSource.Value;

        public virtual IDbContextOptions ContextOptions => _contextOptions;

        public virtual IDataStoreServices DataStoreServices => _dataStoreServices.Value;

        public virtual IServiceProvider ServiceProvider => _provider;

        public virtual void Dispose() => (_provider as IDisposable)?.Dispose();
    }
}
