TechSmith Hyde
========================

Object to Entity mapper for Windows Azure (Think ORM for table storage).

### Features
 * Super simple access to Azure Table Storage. Use your existing POCO C# objects and map Table Storage entities to them.
 * Easily unit test your code that accesses Table Storage with an in-memory test double. No complicated mocks or stubs required!
 * Automatically batches large reads & writes to optimize common operations.
 * Handles quirks of your local Azure emulator for you. (Upserts work locally!)

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
var color = tableStorage.Get<Color>( "MyColorsTable", "Red", "Crimson" );

```

More examples and getting started help can be found [on the wiki](https://github.com/TechSmith/hyde/wiki/Getting-Started)

License: BSD 3-Clause, see http://www.opensource.org/licenses/BSD-3-Clause

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
 * Use autocrlf=true `git config --global core.autocrlf true` http://help.github.com/dealing-with-lineendings/
