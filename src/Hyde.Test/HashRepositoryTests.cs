using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.Test
{
   [TestClass]
   public class HashRepositoryTests
   {
      [TestMethod]
      public void GetHash_HashToRetrieveIsInRepository_ExpectHashIsReturned()
      {
         InMemoryTableStorageProvider.ResetAllTables();
         var hashRepository = new HashRepository( new InMemoryTableStorageProvider() );
         hashRepository.AddHash( new Hash
         {
            Url = "a"
         } );
         hashRepository.SaveChanges();

         var result = hashRepository.GetHash( "a" );

         Assert.AreEqual( "a", result.Url );
      }
   }

   public class Hash
   {
      public string Url
      {
         get;
         set;
      }
   }

   public class HashRepository
   {
      readonly string _hashPartitionKey = "hashPartition";
      private readonly TableStorageProvider _tableStorageProvider;

      public HashRepository( TableStorageProvider tableStorageProvider )
      {
         _tableStorageProvider = tableStorageProvider;
      }

      public void AddHash( Hash hashToAdd )
      {
         _tableStorageProvider.Add( string.Empty, hashToAdd, _hashPartitionKey, hashToAdd.Url );
      }

      public Hash GetHash( string hashUrl )
      {
         return _tableStorageProvider.Get<Hash>( string.Empty, _hashPartitionKey, hashUrl );
      }

      public void SaveChanges()
      {
         _tableStorageProvider.Save();
      }
   }
}