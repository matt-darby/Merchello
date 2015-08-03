﻿namespace Merchello.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Merchello.Core.Models;
    using Merchello.Core.Models.Interfaces;
    using Merchello.Core.Models.TypeFields;
    using Merchello.Core.Persistence.Querying;
    using Merchello.Core.Persistence.UnitOfWork;

    using Umbraco.Core;
    using Umbraco.Core.Events;
    using Umbraco.Core.Persistence;

    using RepositoryFactory = Merchello.Core.Persistence.RepositoryFactory;

    /// <summary>
    /// Represents an entity collection service.
    /// </summary>
    internal class EntityCollectionService : IEntityCollectionService
    {
        #region Fields

        /// <summary>
        /// The locker.
        /// </summary>
        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// The unit of work provider.
        /// </summary>
        private readonly IDatabaseUnitOfWorkProvider _uowProvider;

        /// <summary>
        /// The repository factory.
        /// </summary>
        private readonly RepositoryFactory _repositoryFactory;


        /// <summary>
        /// The valid sort fields.
        /// </summary>
        private static readonly string[] ValidSortFields = { "name" };


        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionService"/> class.
        /// </summary>
        internal EntityCollectionService()
            : this(new RepositoryFactory())
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionService"/> class.
        /// </summary>
        /// <param name="repositoryFactory">
        /// The repository factory.
        /// </param>
        internal EntityCollectionService(RepositoryFactory repositoryFactory)
            : this(new PetaPocoUnitOfWorkProvider(), repositoryFactory)
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionService"/> class.
        /// </summary>
        /// <param name="provider">
        /// The provider.
        /// </param>
        /// <param name="repositoryFactory">
        /// The repository factory.
        /// </param>
        internal EntityCollectionService(IDatabaseUnitOfWorkProvider provider, RepositoryFactory repositoryFactory)
        {
            Mandate.ParameterNotNull(provider, "provider");
            Mandate.ParameterNotNull(repositoryFactory, "repositoryFactory");
            _uowProvider = provider;
            _repositoryFactory = repositoryFactory;
        }

        #region Event Handlers

        /// <summary>
        /// Occurs after Create
        /// </summary>
        public static event TypedEventHandler<IEntityCollectionService, Events.NewEventArgs<IEntityCollection>> Creating;


        /// <summary>
        /// Occurs after Create
        /// </summary>
        public static event TypedEventHandler<IEntityCollectionService, Events.NewEventArgs<IEntityCollection>> Created;

        /// <summary>
        /// Occurs before Save
        /// </summary>
        public static event TypedEventHandler<IEntityCollectionService, SaveEventArgs<IEntityCollection>> Saving;

        /// <summary>
        /// Occurs after Save
        /// </summary>
        public static event TypedEventHandler<IEntityCollectionService, SaveEventArgs<IEntityCollection>> Saved;

        /// <summary>
        /// Occurs before Delete
        /// </summary>		
        public static event TypedEventHandler<IEntityCollectionService, DeleteEventArgs<IEntityCollection>> Deleting;

        /// <summary>
        /// Occurs after Delete
        /// </summary>
        public static event TypedEventHandler<IEntityCollectionService, DeleteEventArgs<IEntityCollection>> Deleted;

        #endregion

        /// <summary>
        /// The create entity collection.
        /// </summary>
        /// <param name="entityType">
        /// The entity type.
        /// </param>
        /// <param name="providerKey">
        /// The provider key.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="raiseEvents">
        /// Optional boolean indicating whether or not to raise events
        /// </param>
        /// <returns>
        /// The <see cref="IEntityCollection"/>.
        /// </returns>
        public IEntityCollection CreateEntityCollection(EntityType entityType, Guid providerKey, string name, bool raiseEvents = true)
        {
            var entityTfKey = EnumTypeFieldConverter.EntityType.GetTypeField(entityType).TypeKey;

            return CreateEntityCollection(entityTfKey, providerKey, name, raiseEvents);
        }

        /// <summary>
        /// The create entity collection with key.
        /// </summary>
        /// <param name="entityType">
        /// The entity type.
        /// </param>
        /// <param name="providerKey">
        /// The provider key.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="raiseEvents">
        /// Optional boolean indicating whether or not to raise events
        /// </param>
        /// <returns>
        /// The <see cref="IEntityCollection"/>.
        /// </returns>
        public IEntityCollection CreateEntityCollectionWithKey(
            EntityType entityType,
            Guid providerKey,
            string name,
            bool raiseEvents = true)
        {
            var entityTfKey = EnumTypeFieldConverter.EntityType.GetTypeField(entityType).TypeKey;

            return CreateEntityCollectionWithKey(entityTfKey, providerKey, name, raiseEvents);
        }

        /// <summary>
        /// Saves a single entity collection.
        /// </summary>
        /// <param name="entityCollection">
        /// The entity collection.
        /// </param>
        /// <param name="raiseEvents">
        /// Optional boolean indicating whether or not to raise events
        /// </param>
        public void Save(IEntityCollection entityCollection, bool raiseEvents = true)
        {
            if (raiseEvents)
            if (Saving.IsRaisedEventCancelled(new SaveEventArgs<IEntityCollection>(entityCollection), this))
            {
                ((EntityCollection)entityCollection).WasCancelled = true;
                return;
            }

            using (new WriteLock(Locker))
            {
                var uow = _uowProvider.GetUnitOfWork();
                using (var repository = _repositoryFactory.CreateEntityCollectionRepository(uow))
                {
                    repository.AddOrUpdate(entityCollection);
                    uow.Commit();
                }
            }

            if (raiseEvents)
            Saved.RaiseEvent(new SaveEventArgs<IEntityCollection>(entityCollection), this);
        }

        /// <summary>
        /// Saves a collection of entity collections.
        /// </summary>
        /// <param name="entityCollections">
        /// The entity collections.
        /// </param>
        /// <param name="raiseEvents">
        /// Optional boolean indicating whether or not to raise events
        /// </param>
        public void Save(IEnumerable<IEntityCollection> entityCollections, bool raiseEvents = true)
        {
            var collectionsArray = entityCollections as IEntityCollection[] ?? entityCollections.ToArray();
            if (raiseEvents) Saving.RaiseEvent(new SaveEventArgs<IEntityCollection>(collectionsArray), this);

            using (new WriteLock(Locker))
            {
                var uow = _uowProvider.GetUnitOfWork();
                using (var repository = _repositoryFactory.CreateEntityCollectionRepository(uow))
                {
                    foreach (var collection in collectionsArray)
                    {
                        repository.AddOrUpdate(collection);
                    }

                    uow.Commit();
                }
            }

            if (raiseEvents) Saved.RaiseEvent(new SaveEventArgs<IEntityCollection>(collectionsArray), this);
        }

        /// <summary>
        /// Deletes a single entity collection.
        /// </summary>
        /// <param name="entityCollection">
        /// The entity collection.
        /// </param>
        /// <param name="raiseEvents">
        /// Optional boolean indicating whether or not to raise events.
        /// </param>
        public void Delete(IEntityCollection entityCollection, bool raiseEvents = true)
        {
            if (raiseEvents)
            if (Deleting.IsRaisedEventCancelled(new DeleteEventArgs<IEntityCollection>(entityCollection), this))
            {
                ((EntityCollection)entityCollection).WasCancelled = true;
                return;
            }

            Delete(entityCollection.ChildCollections());

            using (new WriteLock(Locker))
            {
                var uow = _uowProvider.GetUnitOfWork();
                using (var repository = _repositoryFactory.CreateEntityCollectionRepository(uow))
                {
                    repository.Delete(entityCollection);
                    uow.Commit();
                }
            }

            if (raiseEvents) Saved.RaiseEvent(new SaveEventArgs<IEntityCollection>(entityCollection), this);
        }

        /// <summary>
        /// Deletes a collection of entity collections.
        /// </summary>
        /// <param name="entityCollections">
        /// The entity collections.
        /// </param>
        /// <param name="raiseEvents">
        /// Optional boolean indicating whether or not to raise events.
        /// </param>
        public void Delete(IEnumerable<IEntityCollection> entityCollections, bool raiseEvents = true)
        {
            var collectionsArray = entityCollections as IEntityCollection[] ?? entityCollections.ToArray();
            if (!collectionsArray.Any()) return;
            if (raiseEvents)
            Deleting.RaiseEvent(new DeleteEventArgs<IEntityCollection>(collectionsArray), this);

            foreach (var collection in collectionsArray)
            {
                this.DeleteAllChildCollections(collection);
            }

            using (new WriteLock(Locker))
            {
                var uow = _uowProvider.GetUnitOfWork();
                using (var repository = _repositoryFactory.CreateEntityCollectionRepository(uow))
                {
                    foreach (var collection in collectionsArray)
                    {
                        repository.Delete(collection);
                    }

                    uow.Commit();
                }
            }

            if (raiseEvents)
            Deleted.RaiseEvent(new DeleteEventArgs<IEntityCollection>(collectionsArray), this);
        }

        /// <summary>
        /// The get by key.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="IEntityCollection"/>.
        /// </returns>
        public IEntityCollection GetByKey(Guid key)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                return repository.Get(key);
            }
        }

        /// <summary>
        /// The get by entity type field key.
        /// </summary>
        /// <param name="entityTfKey">
        /// The entity type field key.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        public IEnumerable<IEntityCollection> GetByEntityTfKey(Guid entityTfKey)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                var query = Query<IEntityCollection>.Builder.Where(x => x.EntityTfKey == entityTfKey);
                return repository.GetByQuery(query);
            }
        }

        /// <summary>
        /// The get by provider key.
        /// </summary>
        /// <param name="providerKey">
        /// The provider key.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        public IEnumerable<IEntityCollection> GetByProviderKey(Guid providerKey)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                var query = Query<IEntityCollection>.Builder.Where(x => x.ProviderKey == providerKey);
                return repository.GetByQuery(query);
            }
        }

        /// <summary>
        /// The get all.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        public IEnumerable<IEntityCollection> GetAll()
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                return repository.GetAll();
            }
        }

        /// <summary>
        /// The get children.
        /// </summary>
        /// <param name="collectionKey">
        /// The collection key.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        public IEnumerable<IEntityCollection> GetChildren(Guid collectionKey)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                var query = Query<IEntityCollection>.Builder.Where(x => x.ParentKey == collectionKey);
                return repository.GetByQuery(query);
            }
        }

        /// <summary>
        /// The exists in collection.
        /// </summary>
        /// <param name="parentKey">
        /// The parent key.
        /// </param>
        /// <param name="collectionKey">
        /// The collection key.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool ExistsInCollection(Guid? parentKey, Guid collectionKey)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                var query = Query<IEntityCollection>.Builder.Where(x => x.ParentKey == parentKey && x.Key == collectionKey);
                return repository.Count(query) > 0;
            }
        }

        /// <summary>
        /// The get root level entity collections.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        public IEnumerable<IEntityCollection> GetRootLevelEntityCollections()
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                var query = Query<IEntityCollection>.Builder.Where(x => x.ParentKey == null);
                return repository.GetByQuery(query);
            }
        }

        /// <summary>
        /// The get root level entity collections.
        /// </summary>
        /// <param name="entityType">
        /// The entity type.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        public IEnumerable<IEntityCollection> GetRootLevelEntityCollections(EntityType entityType)
        {
            return this.GetRootLevelEntityCollections(
                EnumTypeFieldConverter.EntityType.GetTypeField(entityType).TypeKey);
        }

        /// <summary>
        /// The get root level entity collections.
        /// </summary>
        /// <param name="entityTfKey">
        /// The entity type field key.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        public IEnumerable<IEntityCollection> GetRootLevelEntityCollections(Guid entityTfKey)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                var query = Query<IEntityCollection>.Builder.Where(x => x.ParentKey == null && x.EntityTfKey == entityTfKey);
                return repository.GetByQuery(query);
            }
        }

        /// <summary>
        /// The get from collection.
        /// </summary>
        /// <param name="collectionKey">
        /// The collection key.
        /// </param>
        /// <param name="page">
        /// The page.
        /// </param>
        /// <param name="itemsPerPage">
        /// The items per page.
        /// </param>
        /// <param name="sortBy">
        /// The sort by.
        /// </param>
        /// <param name="sortDirection">
        /// The sort direction.
        /// </param>
        /// <returns>
        /// The <see cref="Page"/>.
        /// </returns>
        public Page<IEntityCollection> GetFromCollection(
            Guid collectionKey,
            long page,
            long itemsPerPage,
            string sortBy = "",
            SortDirection sortDirection = SortDirection.Descending)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                var query = Query<IEntityCollection>.Builder.Where(x => x.ParentKey == collectionKey);
                return repository.GetPage(page, itemsPerPage, query, this.ValidateSortByField(sortBy), sortDirection);
            }
        }

        /// <summary>
        /// The child entity collection count.
        /// </summary>
        /// <param name="collectionKey">
        /// The collection key.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public int ChildEntityCollectionCount(Guid collectionKey)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                var query = Query<IEntityCollection>.Builder.Where(x => x.ParentKey == collectionKey);
                return repository.Count(query);
            }
        }

        /// <summary>
        /// The has child entity collections.
        /// </summary>
        /// <param name="collectionKey">
        /// The collection key.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool HasChildEntityCollections(Guid collectionKey)
        {
            return this.ChildEntityCollectionCount(collectionKey) > 0;
        }

        /// <summary>
        /// The create entity collection.
        /// </summary>
        /// <param name="entityTfKey">
        /// The entity type field key.
        /// </param>
        /// <param name="providerKey">
        /// The provider key.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="raiseEvents">
        /// Optional boolean indicating whether or not to raise events
        /// </param>
        /// <returns>
        /// The <see cref="IEntityCollection"/>.
        /// </returns>
        internal IEntityCollection CreateEntityCollection(Guid entityTfKey, Guid providerKey, string name, bool raiseEvents = true)
        {
            Mandate.ParameterCondition(!Guid.Empty.Equals(entityTfKey), "entityTfKey");
            Mandate.ParameterCondition(!Guid.Empty.Equals(providerKey), "providerKey");
            var collection = new EntityCollection(entityTfKey, providerKey) { Name = name };

            if (Creating.IsRaisedEventCancelled(new Events.NewEventArgs<IEntityCollection>(collection), this))
            {
                collection.WasCancelled = true;
                return collection;
            }

            return collection;
        }

        /// <summary>
        /// The create entity collection with key.
        /// </summary>
        /// <param name="entityTfKey">
        /// The entity type field key.
        /// </param>
        /// <param name="providerKey">
        /// The provider key.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="raiseEvents">
        /// Optional boolean indicating whether or not to raise events
        /// </param>
        /// <returns>
        /// The <see cref="IEntityCollection"/>.
        /// </returns>
        internal IEntityCollection CreateEntityCollectionWithKey(Guid entityTfKey, Guid providerKey, string name, bool raiseEvents = true)
        {
            var collection = this.CreateEntityCollection(entityTfKey, providerKey, name, raiseEvents);

            if (((EntityCollection)collection).WasCancelled) return collection;

            using (new WriteLock(Locker))
            {
                var uow = _uowProvider.GetUnitOfWork();
                using (var repository = _repositoryFactory.CreateEntityCollectionRepository(uow))
                {
                    repository.AddOrUpdate(collection);
                    uow.Commit();
                }
            }

            if (raiseEvents)
                Created.RaiseEvent(new Events.NewEventArgs<IEntityCollection>(collection), this);

            return collection;
        }

        /// <summary>
        /// The get entity collections by product key.
        /// </summary>
        /// <param name="productKey">
        /// The product key.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        internal IEnumerable<IEntityCollection> GetEntityCollectionsByProductKey(Guid productKey)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                return repository.GetEntityCollectionsByProductKey(productKey);                
            }
        }

        /// <summary>
        /// The get entity collections by invoice key.
        /// </summary>
        /// <param name="invoiceKey">
        /// The invoice key.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        internal IEnumerable<IEntityCollection> GetEntityCollectionsByInvoiceKey(Guid invoiceKey)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                return repository.GetEntityCollectionsByInvoiceKey(invoiceKey);
            }
        }

        /// <summary>
        /// The get entity collections by customer key.
        /// </summary>
        /// <param name="customerKey">
        /// The customer key.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{IEntityCollection}"/>.
        /// </returns>
        internal IEnumerable<IEntityCollection> GetEntityCollectionsByCustomerKey(Guid customerKey)
        {
            using (var repository = _repositoryFactory.CreateEntityCollectionRepository(_uowProvider.GetUnitOfWork()))
            {
                return repository.GetEntityCollectionsByCustomerKey(customerKey);
            }
        }

        /// <summary>
        /// The validate sort by field.
        /// </summary>
        /// <param name="sortBy">
        /// The sort by.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string ValidateSortByField(string sortBy)
        {
            return ValidSortFields.Contains(sortBy.ToLowerInvariant()) ? sortBy : "name";
        }

        /// <summary>
        /// The delete all child collections.
        /// </summary>
        /// <param name="collection">
        /// The collection.
        /// </param>
        private void DeleteAllChildCollections(IEntityCollection collection)
        {
            if (!this.HasChildEntityCollections(collection.Key)) return;

            Delete(collection.ChildCollections());
        }
    }
}