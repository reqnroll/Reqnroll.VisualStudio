# Reqnroll Binding Data

This directory contains the binding data structure for the Reqnroll provider. 

The classes here are copied from https://github.com/reqnroll/Reqnroll/tree/main/Reqnroll/Bindings/Provider/Data.

In case there is a breaking change in the Reqnroll binding data structure, the data structure versions that should be supported should be imported here and a new implementation of the `IBindingProvider` interface has to be created and the right one should be selected in the `DiscoveryExecutor` class based on the Reqnroll version.