# README

DemoCentralTests has unit tests for every working class in DemoCentral.*

**CAREFUL** Currently implemented are only the tests for [`DemoCentralDBInterface.cs`](../DemoCentral/DemoCentralDBInterface.cs)
[`InQueueDBInterface.cs`](../DemoCentral/InQueueDBInterface.cs)


# `Testformat`
 
 The  tests are intentionelly kept short with overly descriptive names, like `GetRecentMatchesDoesNotFailIfRequestingMoreMatchesThanExist()`.
 Again, it's done intentionelly, because it helps tremendously in the Visual Studio test explorer.
   
# Tricks

 <b> if you are testing a DataBaseInterface, make sure to use a fresh DataBase after each test.
```
        [TestCleanup]
        public void DropDataBase()
        {
            using (var context = new DemoCentralContext(_test_config))
                context.Database.EnsureDeleted();
        } 
```

<b> If you are working with code-first, make sure to use a new object every time. For this, create a prototype and make a deep copy of it. 
```
//CopyDemo() creates deep copy
Demo demo = CopyDemo(_standardDemo);
```


 <b> when testing a code-first DB, you can reuse the model to check its variables, instead of requesting the data again.
 ```
			
			//Create a model 
            Demo demo = CopyDemo(_standardDemo);
            var new_hash = "new_hash";

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueTableInterface);
                AddDemoToDB(demo, context);

                //Update its hash
                //Make sure to call context.SaveChanges()
                test.UpdateHash(demo.MatchId, new_hash);
            }

			//Compare with the originally created model
            Assert.AreEqual(new_hash, demo.Md5hash);
 ``` 
 The model created, in this case `Demo demo` keeps its fields. So if you call context.SaveChanges() to save the fields to the DataBase, the fields from your model get updated too. If you want to check whether the `demo.Md5hash` was updated, take a look at the models fields.

<b> If you want to check whether an exception gets thrown, the MSTest framework requires a Func<>.  Create one this way:
```
Assert.ThrowsException<InvalidOperationException>(() => test.UpdateHash(unknown_matchId, new_hash));
```

<b> Dont verify twice. If you are testing a higher-level function, mock the lower-levels and check only if they get called.

```
using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, mockedObject);

                test.TryCreateNewDemoEntryFromGatherer(model, out matchId);
            }
			
			//Check if the `mockedObject.Add()` gets called with these parameters
            mockInQueueDB.Verify(mockedObject => mockedObject.Add(matchId, matchDate, Source.Unknown, uploaderId), Times.Once());
```

<b>To minimize dependency between tests, add demos manually to the context, instead of relying on your build Add() function. Otherwise these get tested unintentionelly often. 
```
	//Use
	context.Demo.Add(demo);
	context.SaveChanges();

	//Instead of 
	test.Add(matchId, new DateTime(), Source.Faceit, 1234);
```
	
