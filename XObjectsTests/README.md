# XObjectsTests

This project contains unit and integration tests for the XObjectsCore library.

**Almost all of the tests are integration tests** as actual unit tests were not written for the library back when Microsoft was first developing it. Even when it was open sourced a few years later it was not covered by a very extensive testing suite. As most of the value of the library lies in its code generation facilities, the tests mostly cover that the code generated actually works as expected.