using Xunit;

// GameEvent uses a static, resettable ID counter (GameEvent.ResetIdCounter) that several test
// classes reset in their ctor/Dispose. Running test collections in parallel lets one class's reset
// race another's event creation, colliding IDs and breaking ID-keyed engine logic (e.g. the
// once-per-event fundamental-impact gate). Serializing the suite removes the race; the suite is
// small enough that the wall-clock cost is negligible.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
