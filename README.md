TechSmith Hyde
========================

Object to Entity mapper for Windows Azure, it **hy**drates and **de**hydrates objects to and from storage (Think ORM for table storage).

### Features
 * Super simple access to Azure Table Storage. Use your existing POCO C# objects and map Table Storage entities to them.
 * Easily unit test your code that accesses Table Storage with an in-memory test double. No complicated mocks or stubs required!
 * Automatically batches large reads & writes to optimize common operations.
 * Easily compose complex queries using the fluent interface.
 * Handles quirks of your local Azure emulator for you.

### Basic Example

```csharp
using TechSmith.Hyde;
using TechSmith.Hyde.Table;

public class Color
{
  public string HexColorCode
  {
    get;
    set;
  }
}

var storageAccount = new ConnectionStringCloudStorageAccount( "YourConnectionStringHere" );
var tableStorage = new AzureTableStorageProvider( storageAccount );

var color = tableStorage.CreateQuery<Color>( "MyColorsTable" )
                        .PartitionKeyEquals( "Red" )
                        .RowKeyEquals( "Crimson" );

```

More examples and getting started help can be found [on the wiki](https://github.com/TechSmith/hyde/wiki/Getting-Started)

###License
BSD 3-Clause, see http://www.opensource.org/licenses/BSD-3-Clause

### Getting started with development
 1. The first step is to get your developer machine setup. The easiest way to do so is to [run the machine setup script](tools/machinesetup.bat)
 1. Finally, farmiliarize yourself with the submission guidelines below.

### Submission guidelines
Follow these guidelines, in no particular order, to improve your chances of having a pull request merged in.

 * Add an issue for what you plan to add/improve/fix in the project to start a discussion prior to submitting the code.
 * Include unit and/or integration tests with code submissions.
 * Make each pull request atomic and exclusive; don't send pull requests for a huge list of changes.
 * Even better, commit in small manageable chunks.
 * Spaces, not tabs. Use 3 spaces. Brackets should be on their own line.
 * No regions
 * Code must build .NET 4.
 * If you didn't write the code you must provide a reference to where you obtained it and the license. 

### FAQ - Technical Details 
**Looking at my tables in Azure Table Storage, I notice that some fields contain the prefix "%HYDE_DATETIME%" while others do not. What's up with that?**  
The C# DateTime struct supports a range from year 0001 to year 9999, while Table Storage only supports DateTimes from year 1601 to year 9999. To work around 
this, if we cannot store a C# DateTime using a corresponding Table Storage DateTime, we store it as a string and tag it with its corresponding C# structure so that
we can retrieve it as the appropriate type in the future.
