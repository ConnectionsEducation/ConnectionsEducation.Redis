# ConnectionsEducation.Redis

The library is a custom implementation of the [Redis protocol](http://redis.io/topics/protocol),
as a client library for the .NET Framework, version 4.0. The library is implemented without any
external dependencies (the primary motivation for development). If your application can target
the .NET Framework, version 4.5, then you might use a more advanced library, such as
[StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis/).

Please refer to the LICENSE file for copyright and license information.

## Getting Started

Please refer to the unit tests for a comprehensive example of usage. Most Redis commands follow
the [command syntax](http://redis.io/commands) closely, available as methods of a `Redis` instance.

Example:
```csharp
using (Redis redis = new Redis()) {
	redis.set("foo", "bar");
	redis.set("bar", "foo");
	redis.set("baz", "hello world");
	redis.hset("fuzz", "a", "apple");
	redis.hset("fuzz", "b", "banana");

	string[] keys = redis.keys("*z");
}
```

Where possible, simple C# idioms have been used to faciliate passing complex types.

Example:
```csharp
using (Redis redis = new Redis()) {
	redis.del("foo");
	redis.zadd("foo", Tuple.Create(2L, "bar2"), Tuple.Create(1L, "bar1"));
	string[] range = redis.zrange("foo", 0);
}
```

The library does not attempt to be too "smart" regarding more complex commands such as [SCAN](http://redis.io/commands/scan).
For example, in that case, the SCAN command returns a simple object containing the next cursor, and the results of the current iteration.
It does not avoid duplicates.

Example:
```csharp
using (Redis redis = new Redis()) {
	Redis.ScanResult result = null;
	string[] keys = {
		"key1", "key2", "key3", "key4", "test1", "test2", "test3", "test4",
		"key5", "key6", "key7", "key8", "test5", "test6", "test7", "test8",
		"foo1", "foo2", "foo3", "foo4", "fuzz1", "fuzz2", "fuzz3", "fuzz4",
		"foo5", "foo6", "foo7", "foo8", "fuzz5", "fuzz6", "fuzz7", "fuzz8",
	};
	foreach (string key in keys) {
		redis.set(key, "scantest");
	}

	int iterations = 0;
	do {
		result = redis.scan(result == null ? 0L : result.nextCursor);
		iterations += 1;
		string[] results = result.results;
	} while (result.nextCursor != 0);
}
```

Explore the code, run the tests, and contribute!
