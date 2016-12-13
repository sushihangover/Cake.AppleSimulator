## Cake.AppleSimulator.SushiHangover
 [![Build status](https://ci.appveyor.com/api/projects/status/s6u8up9vypcwg2vs/branch/sushi?svg=true)](https://ci.appveyor.com/project/sushihangover/cake-applesimulator/branch/sushi)

Cake Build addin for managing (simctl) Apple iOS/watchOS/TV simulators.

## Installation

Add the following reference to your cake build script:

```csharp
#addin "Cake.AppleSimulator"
```


## Usage

```csharp
// erase/factory reset, then power on an iPhone 6.3 simulator running iOS 9.3 and then shut it down.
IReadonlyList<AppleSimulator> simulator = ListGenymotionSimulators();

var simulator = simulators.First(x => x.Name == "iPhone 6s" & x.Runtime == "iOS 9.3");
var deviceIdentifier = simulator.UDID;

EraseAppleSimulator(deviceIdentifier);

// launch simulator currently does not block, you will need to sleep the cake
// thread for enough time to allow the simulator to boot.
LaunchAppleSimulator(deviceIdentifier);

using System.Threading;
Thread.Sleep(15000);

ShutdownAllAppleSimulators(deviceIdentifier)
```

### Administration

```csharp
void EraseAppleSimulator(string deviceIdentifier);
void BootAppleSimulator(string deviceIdentifier)
void LaunchAppleSimulator(string deviceIdentifier)
void ShutdownAllSimulators();
```

### List defined simulators

```csharp
IReadonlyList<AppleSimulator> ListAppleSimulators();

public sealed class AppleSimulator
{
    public string Availability { get; set; }
    public string Name { get; set; }
    public string Runtime { get; set; }
    public string State { get; set; }
    public string UDID { get; set; }
}
```

### List defined simulator pairings

```csharp
IReadonlyList<AppleSimulatorPair> ListAppleSimulatorPairs();

public sealed class AppleSimulatorPair
{
    public string State { get; set; }
    public string UDID { get; set; }
    public AppleSimulatorPairedPhone Phone { get; set; }
    public AppleSimulatorPairedWatch Watch { get; set; }
}

public sealed class AppleSimulatorPairedPhone
{
    public string Name { get; set; }
    public string State { get; set; }
    public string UDID { get; set; }
}

public sealed class AppleSimulatorPairedWatch
{
    public string Name { get; set; }
    public string State { get; set; }
    public string UDID { get; set; }
}
```


### List installed simulator runtimes
```csharp
IReadonlyList<AppleSimulatorRuntime> ListAppleSimulatorRuntimes();

public sealed class AppleSimulatorRuntime
{
    public string Availability { get; set; }
    public string BuildVersion { get; set; }
    public string Identifier { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
}
```

### List installed simulator devicetypes
```csharp
IReadonlyList<AppleSimulatorDeviceType> ListAppleSimulatorDeviceTypes();

public sealed class AppleSimulatorDeviceType
{
    public string Identifier { get; set; }
    public string Name { get; set; }
}
```

## The future
The infrastructure for the following is in place but not implemented, open an issue on GitHub, discuss and then send in a PR. Cheers!

* Add video/photos to asset libary.
* Install application onto simulator.
* Uninstall application from simulator.
* Launch application on simulator.
* Configure simulator to disable keyboard autocorrect/etc
* Configure simulator graphics quality.
* Configure simulator window scale.
* Retrieve physical location on disk of the simulator's app container.
* Trigger an iCloud sync.
* Pair an iPhone+Watch.
* Unpair an iPhone+Watch.
* Open URL on a simulator.
