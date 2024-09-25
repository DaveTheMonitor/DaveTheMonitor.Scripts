# CSRScript2

## What is CSRScript2?

CSRScript2 is a simple scripting language based on Total Miner's TMScript. It is designed to be as easy as possible to learn for those who already know TMScript. While most basic scripts will translate almost 1:1, some scripts can look very different due to changes.

If you're already familiar with TMScript and just want to know what's different, you can view the [Differences From TMScript](#differences-from-tmscript) section.

## Why does CSRScript exist?

CSRScript exists to be used by mods. C# mods can very easily add new functionality to CSRScript, such as methods and objects. In the future, CSR will allow CSRScript to be used by and embedded in XML for greater XML modding capability.

## How to use CSRScript?

Currently CSRScript can only be used in the sandbox app. A standalone mod that allows using CSRScript in-game will be released soon.

Unless otherwise specified, you can run any script shown here to see the results. Try changing them to see what happens!

## Contents

## Comments

Comments in scripts are ignored. This allows you to document what something does or why it does it in the script itself.

Single-line comments are prefixed with `//` or `#`, and take up the entire line after they start.

```csharp
// This is a single-line comment.

# This is also a single-line comment.
```

Multi-line comments start with `/*` and end with `*/`. These comments can span multiple lines.

```csharp
/* This is a multi-line comment
that takes two lines */
```

## Statements And Expressions

Each line in CSRScript is a statement. Some statements may expect expressions. Expressions return some value, and can be in other expressions. Every statment must be on its own line. Here are some examples of statements:

```csharp
// This prints 100 to the console.
// 'Print' is the function.
// '[100]' is the argument, which is an expression.
Print [100]

// This sets the local variable 'x' to be equal to 40.
Var [x] = [40]

// This sets the local variable 'y' to be equal to
// 'x' + 10.
Var [y] = [x] + [10]

// This prints the local variable 'y' to the console.
Print [y]

// This prints the local variable 'x' - 10 to the
// console.
Print [[x] - [10]]
```

Expressions can be:
- A literal value (eg. `[10]`, `["Hello, world!"]`)
    - String literals must be enclosed in quotation marks.
- A variable (eg. `[x]`)
    - Variables can be declared with `Var` or `In`. Variables declared with `Var` can be changed at any time, variables declared with `In` cannot.
- A property getter (eg. `[Pi:]`, `[actor:Health]`)
    - Static property getters always end with a `:`, while object property getters are formatted as `[object:property]`
- A function or method call (eg. `Min [0] [10]`)
    - Functions and methods will be explained in more detail shortly.

Did you notice that both the `[y]` variable declaration and print statement use 2 expressions, but only the print statement had both enclosed in outer brackets? This isn't a mistake. In most cases, expressions must be enclosed in outer brackets, but sometimes that isn't the case. Multiple expressions do not need to be enclosed in outer brackets in the following statements:
- If
- While
- For

These statements will be explained in more detail shortly.

## Variables

In CSRScript, there are two types of variables. Input Variables, or 'InVars' for short, and Local Variables. InVars are declared using the `In` keyword, and cannot be changed after declaration. Local variables are declared using the `Var` keyword, and can be changed at any time.

All variable, function, and property names are case-insensitve. This means `[MyVar]` and `[myvar]` are considered the same variable. `[Random]` and `[random]` refer to the same function.

Local variables are dynamic, meaning their type can change at any time during execution. In the following example, `[x]` is set to `[10]`, which is a number, and then later set to `["Hello!"]`, which is a string.

```csharp
// Set [x] to 10.
Var [x] = [10]

Print [x]

// Set [x] to "Hello!".
Var [x] = ["Hello!"]

Print [x]
```

InVars are not dynamic, as their value cannot be changed. They can, however, be any type, including an unknown (dynamic) type. InVars can be strongly typed if you know what their type will be when the script is called. For example, if the script is called when an actor takes damage, then `[self]` will always be an actor.

```csharp
// [self] in this case is strongly typed as an "Actor".
// If the script tries to call a method or property that
// doesn't exist on Actor, the script won't compile.
In [Actor:self]

// [target] in this case is not strongly typed.
// This means it can be any type. Invalid method and
// property calls will still compile, but will throw a
// runtime error.
In [target]
```

It is highly recommended to strongly type InVars if possible as it helps when writing the script, and is also better for performance. More information on this later.

If you're familiar with TMScript, you can think of InVars as contexts. CSRScript doesn't have contexts like TMScript does, instead actors are variables. The `[self]` InVar would be the `[default]` context actor in TMScript.

Multiple InVars can be declared on the same line:

```csharp
// This script will not compile as the TotalMiner namespace
// isn't available in the sandbox.
Using [TotalMiner]
In [Actor:self] [Actor:target]
```

## Functions And Properties

Functions and properties can be static or instance. Instance functions are known as Methods. Static functions and properties can be called by any script at any time, while methods and instance properties require an object to call on.

To get the value of an static property, you can use the property name followed by a `:`.

```csharp
// The mathematical constant Pi, or 3.14159274.
Var [pi] = [Pi:]

Print [pi]
```

Functions without arguments are called like property getters.

```csharp
// Creates a new empty array and stores a reference to it
// in [array].
Var [array] = [CreateArray:]

// output: "array[0]"
Print [array]

// Creates a new empty array but does not store a reference
// to it.
CreateArray:
```

Functions with arguments can be called by passing each argument in brackets after the function name, without a `:`. Arguments are expressions, so they can contain other function and property calls, math operators, and anything else an expression can contain.

```csharp
// Random returns a decimal number between the first (min)
// and second (max) arguments.
Var [result] = [Random [0] [10]]

Print [result]

// RandomInt returns a whole number between the first (min)
// and second (max) arguments, min inclusive, max exclusive.
Var [result] = [RandomInt [0] [10]]

Print [result]
```

Methods and instance properties can be called by specifying the variable identifier and the method or property name.

```csharp
// Create a new empty array.
Var [array] = [new Array]

// Adds the value [10] to the array.
array:Add [10]

// Gets the count property from the array, which is how
// many items are currently in the array.
Var [count] = [array:Count]

Print [count]

// Clears the array.
array:Clear

// Count will now be 0.
Var [count] = [array:Count]

Print [count]
```

## Conditionals

CSRScript supports if statements to execute code conditionally. In the following example, we print if `[x] == [0]`

```csharp
// Because RandomInt is max exclusive, the result will be
// either 0 or 1.
Var [x] = [RandomInt [0] [2]]

If
    [x] == [0]
Then
    // The following code will only execute if [x] is 0...
    Print ["x == 0"]
Else
    // ...otherwise this code will execute.
    Print ["x != 0"]
End
```

Multiple if statements can be nested.

```csharp
Var [x] = [RandomInt [0] [2]]

If
    [x] == [0]
Then
    // This code runs if [x] == 0

    Var [y] = [Random [0] [1]]
    If
        [y] < [0.5]
    Then
        // This code runs if [y] < 0.5
        Print ["y < 0.5"]
    Else
        // This code runs if [y] >= 0.5
        Print ["y >= 0.5"]
    End
Else
    // And this code runs if [x] != 0
    Print ["x != 0"]
End
```

Note that conditions and loops have their own scopes. They can modify variables declared outside of them, but variables declared inside of them cannot be used outside of them.

```csharp
// [x] is being declared globally. Any scope in this script
// can use it and modify its value.
Var [x] = [Random [0] [1]]

If
    [x] < [0.5]
Then
    // This is valid because [x] is global to this script.
    Var [x] = [0]
    
    // We also declare [y] here
    Var [y] = [1]
End

// This won't compile, because [y] is declared in a
// different scope.
Print [y]
```

If you want a variable's value to depend on a condition, declare it outside of the condition's scope and set it inside of the condition.

```csharp
Var [x] = [Random [0] [1]]

// We declare [y] here, setting it to null to indicate that
// it isn't initialized yet.
Var [y] = [null]

If
    [x] < [0.5]
Then
    Var [y] = [0]
Else
    Var [y] = [1]
End

// [y] will now be either 0 or 1, depending on the result
// of the random call.
Print [y]
```

If statements can also test multiple conditions with the `and` and `or` boolean logical operators. `&&` and `||` can also be used instead of `and` and `or` respectively.

Boolean logical operators are evaluated last. In other words, everything to the left and right will be evaluated before the boolean logical operator. Both `and` and `or` have equal precedence and are evaluated left-right.

```csharp
Var [x] = [8]
Var [y] = [5]

// Outputs "Condition true"
If
    [x] == [10] or [y] == [5]
Then
    Print ["Condition true"]
Else
    Print ["Condition false"]
End
```

```csharp
// Here we create a new empty array.
Var [array] = [new Array]

// Next we add 10 to it.
array:Add [10]

// If the array contains 10, we print "Array contains [10]"
// Otherwise, we print "Array does not contain [10]"
If
    // The Contains method returns True if the array
    // contains the specified value, otherwise false.
    // Because the expression returns a boolean,
    // '== [true]' is assumed so we don't have to write it.
    // That means this is equivalent to
    // '[array:Contains [10]] == [true]'
    [array:Contains [10]]
Then
    Print ["Array contains [10]"]
Else
    Print ["Array does not contain [10]"]
End
```

## Loops

CSRScript supports While and For loops, as well as a Loop statement. Multiple loops can be nested and have their own scope, like If statements. Loops can be broken using the `Break` statement, and an iteration can be skipped using the `Continue` statement.

For loops consist of an initializer, condition, iterator, and body. The initializer is executed once before the loop starts, the condition is executed at the start of each loop, and the iterator is executed at the end of each loop.

The following example is a For loop, which increments [x] and prints it 10 times.

```csharp
For
    // This is the initializer. Here we declare [i].
    // Note that [i] is local to the loop and cannot be
    // used outside of it.
    // It's common for a For loop counter to be named [i]
    Var [i] = [0]

    // This is the condition. Here we test if [i] < 10, and
    // if it is, continue with the loop.
    [i] < [10]

    // This is the iterator. Here we increment [i]. The
    // '++' operator is equivalent to `Var [i] = [i] + [1]`
    Var [i]++
Do
    // This is the loop body. Here we print [i]
    Print [i]
End

// This script outputs all numbers from 0-9.
```

While loops consist of a condition and a body. The condition is run at the start of every loop.

The following example is a While loop, which sets `[x]` to a random number between 0-10, max exclusive, until it's generated 5 numbers or `[x] == 5`, whichever happens first.

```csharp
// We declare [x] outside of the loop so we can print it
// after the loop.
Var [x] = [0]

// We also keep track of how many times we've looped. This
// must be outside of the loop.
Var [loops] = [0]

While
    // This is the condition. Here we test if [x] != 5, and
    // if it isn't, continue with the loop.
    [x] != [5]
Do
    // Here we test if [loops] == [5], and if it is, break
    // out of the loop.
    If
        [loops] == [5]
    Then
        Print ["Reached 5th loop."]
        Break
    End

    // This is the loop body. Here we set [x] to a random
    // number between 0-10, max exclusive.
    Var [x] = [RandomInt [0] [10]]

    Var [loops]++
End

Print [x]
```

Loop statements consist of a count and body. Unlike other loops, Loop statements must have the count expression on the same line, and do not require a `Do` statement.

The following example loops 10 times, generating a random number between 1-100 each loop.

```csharp
// Loop 10 times.
Loop [10]
    // Set [x] to a random number between [0] and [100],
    // max exclusive, and add 1.
    Var [x] = [RandomInt [0] [100]] + [1]

    // Print the result.
    Print [x]
End
```

One common use of a For loop is to iterate over an array and use each of its values. The following example fills an array with 10 random numbers, and iterates over it, printing each value.

```csharp
// First we create an empty array.
Var [array] = [new Array]

// Now we add 10 random numbers between 0-1 to it.
Loop [10]
    array:Add [Random [0] [1]]
End

// Now we iterate over the array using a For loop.
For
    // We declare our counter.
    Var [i] = [0]

    // We test that [i] < the number of items in the array.
    [i] < [array:Count]

    // And we increment [i] at the end of the loop.
    Var [i]++
Do
    // The ItemAt method returns the item at the specified
    // index of the array.
    // Note that arrays start at 0, not 1. That's why we
    // start with [i] = [0] instead of [i] = [1], and end the
    // loop when it's equal to the number of items, rather
    // than greater than the number of items
    Var [item] = [array:ItemAt [i]]
    
    // Now we print the item at that index.
    Print [item]
End
```

There's actually a separate loop specifically for this, the Foreach loop. You can use the Foreach loop to iterate over an array, doing something with each item in it. The below code is identical to the above code.

```csharp
// First we create an empty array.
Var [array] = [new Array]

// Now we add 10 random numbers between 0-1 to it.
Loop [10]
    array:Add [Random [0] [1]]
End

// Now we iterate over the array using a Foreach loop
Foreach
    // [item] will be what Var [item] = [array:ItemAt [i]]
    // would have been.
    // The name of [item] doesn't matter. For example, if
    // the array contains only actors, you could call it
    // [actor] instead.
    Var [item] in [array]
Do
    // Now we print the item
    Print [item]
End
```

## Types

Variables can be one of many types, but are dynamic, meaning which type that is may change during execution. There are 5 common types:

| Type   | Example             |
|--------|---------------------|
| Number | `[10], [0.5]`       |
| Bool   | `[true], [false]`   |
| String | `["Hello, world!"]` |
| Null   | `[null]`            |
| Object |                     |

Null is special, in that it represents the absence of any value. A function or method may return null instead of what they are expected to return. Sometimes this will have to be dealt with accordingly.

Object can be one of several other types, such as an array, actor, etc.

### Testing Types at Runtime

Though it's uncommon, if you need to know the type of a variable at runtime, use the GetType and IsType functions. This is more technical, so if it doesn't make much sense, that's fine. You probably won't need to do this anyway.

These functions require the full type names, including the namespace and the type, as `Namespace.TypeName`. All default types are in the `scripts` namespace. So the full name of the Array type is `Scripts.Array`

```csharp
Var [array] = [new Array]

// Returns the string representation of the type of [array]
// In this case, that's 'scripts.array'
Var [typeName] = [GetType [array]]

Print [typeName]

// Returns the numeric ID of the type of [array]. This ID
// can change each time the script is run, so don't store
// it for use later, only use it in the same script that
// called GetTypeId.
Var [typeId] = [GetTypeId [array]]

Print [typeId]

// Returns True if the value is the specified type or a
// subtype of the specified type. Note that different
// array types are not subtypes of 'scripts.array'
Var [result] = [IsType [array] ["scripts.array"]]

Print [result]

// If you have already have the ID of a type, you can also
// use the IsTypeId function, which is faster than IsType.
Var [result] = [IsTypeId [array] [typeId]]

Print [result]
```

So if you want to execute code only if the result of a function is a specific type, you can use these functions.

```csharp
// This code will always output "item is not a number or
// string" in the sandbox, because [item] will always be
// null.
// Try changing 'In [item]' to 'Var [item] = [10]' or
// 'Var [item] = [""]' to see how the output changes.
In [item]

If
    [IsType [item] ["scripts.number"]]
Then
    Print ["item is a number"]
ElseIf
    [IsType [item] ["scripts.string"]]
Then
    Print ["item is a string"]
Else
    Print ["item is not a number or string"]
End
```

## Using

Scripts can use the `Using` keyword to specify namespaces to use by default when using strong-typing. Currently, strong-typing can only be used with InVars. Strong Typing can improve performance and allows the compiler to warn you of type errors before before running the script.

The `Scripts` namespace is used by all scripts by default, so `Using [Scripts]` doesn't need to be included. Including a namespace with `Using` allows using that namespace's types and functions by just using the name. If a namespace isn't included with `Using`, the full type or function name must be used, written as `namespace.name`

```csharp
// This script will not compile as the TotalMiner namespace
// isn't available in the sandbox.

// No Using
In [TotalMiner.Actor:self]

Var [x] = [self:X]
Var [y] = [self:Y] - [1]
Var [z] = [self:Z]

// The GetBlock function returns ID of the block at the
// specified coordinates.
Var [block] = [TotalMiner.GetBlock [x] [y] [z]]
If
    // Tests if the actor is standing on Basalt
    [block] == ["Basalt"] and [self:Grounded]
Then
    // The Damage Actor method deals damage to the actor.
    self:Damage [1] [False]
End
```

```csharp
// This script will not compile as the TotalMiner namespace
// isn't available in the sandbox.

// Using
Using [TotalMiner]
In [Actor:self]

Var [x] = [self:X]
Var [y] = [self:Y] - [1]
Var [z] = [self:Z]

// The GetBlock function returns ID of the block at the
// specified coordinates.
Var [block] = [GetBlock [x] [y] [z]]
If
    // Tests if the actor is standing on Basalt
    [block] == ["Basalt"] and [self:Grounded]
Then
    // The Damage Actor method deals damage to the actor.
    self:Damage [1] [False]
End
```

## Objects

Objects are different from other types. Strings are technically objects, but don't have to be treated as one. Objects are references, meaning that two different variables can point to the same object, and modifying one will modify the other. Some objects can be created using a constructor, while others cannot. Constructors are called using the `new` keyword. In the following example, the Array constructor is called, which returns a new array object.

```csharp
// Create an array and reference it with the [array_1]
// variable.
Var [array_1] = [new Array]

// Set [array_2] to reference the same array as [array_1]
Var [array_2] = [array_1]

// Contains returns True if the array contains the
// specified value, otherwise False.
// Because the array is empty, this will return False.
Var [result] = [array_2:Contains [10]]

// output: False
Print [result]

// Add 10 to the array.
array_1:Add [10]

// Call Contains on [array_2] again. Now, because we added
// 10 to [array_1], which is the same array referenced by
// [array_2], the method returns True.
Var [result] = [array_2:Contains [10]]

// Output: True
Print [result]

// Now we create a new array again. This array is
// separate from the original array.
// [array_1] will reference this new array, while
// [array_2] will still reference the old array.
Var [array_1] = [new Array]

// Because the new array is empty, this will return False.
Var [result] = [array_1:Contains [10]]

// Output: False
Print [result]

// Because [array_2] still references the old array, this
// will return True.
Var [result] = [array_2:Contains [10]]

// Output: True
Print [result]
```

If you're familiar with TMScript, objects replace contexts. Instead of using the `Context` command to switch to a context, in CSRScript that 'context' is an InVar object.

## String Concatenation

Strings can be concatenated using the `+` operator. If the lefthand side of the operator is a string, the righthand side will be converted to a string.

```csharp
// Declare the first name
Var [firstName] = ["John"]

// Declare the last name
Var [lastName] = ["Smith"]

// Concatenate them together to form the full name.
// Because concatenation does not add anything between the
// strings, we must add the space ourselves.
Var [fullName] = [firstName] + [" "] + [lastName]

// Output: John Smith
Print [fullName]
```

## String Functions

There are several built-in functions for strings. A few common ones include:
- Substring - Returns part of a string.
- StringLength - Returns the length of a string.
- StringContains - Returns True if the string contains the specified value.

A full list of string functions and their arguments can be found in the built-in functions list.

## Built-In Types

This a list of all built-in types, and their methods. A return type of `void` means the method doesn't return anything (null if a variable is set to the return value)

### Array

All `Array` types have the same methods. The `T` type refers to the type contained in the array, which is `Dynamic` for arrays created with the constructor.

| Type        | Name       | Returns  | Arguments              |
|-------------|------------|----------|------------------------|
| Property(G) | `ReadOnly` | `bool`   |                        |
| Property(G) | `Count`    | `int`    |                        |
| Method      | `Add`      | `void`   | `[T:item]`             |
| Method      | `Insert`   | `void`   | `[T:item] [int:index]` |
| Method      | `Remove`   | `bool`   | `[T:item]`             |
| Method      | `RemoveAt` | `void`   | `[int:index]`          |
| Method      | `Insert`   | `void`   | `[T:item] [int:index]` |
| Method      | `Clear`    | `void`   |                        |
| Method      | `Contains` | `bool`   | `[T:item]`             |
| Method      | `IndexOf`  | `int`    | `[T:item]`             |
| Method      | `ItemAt`   | `T`      | `[int:index]`          |
| Method      | `Copy`     | `array`  |                        |

### Random

`Random` objects can be used to generate random numbers. `Random` objects can be created with a constructor.

| Type        | Name       | Returns  | Arguments                   |
|-------------|------------|----------|-----------------------------|
| Property(G) | `Seed`     | `int`    |                             |
| Method      | `NextInt`  | `int`    | `[int:min] [int:max]`       |
| Method      | `Next`     | `number` | `[number:min] [number:max]` |

## Built-In Functions

This is a list of all built-in functions and their arguments and return types. A return type of `void` means the function doesn't return anything (null if a variable is set to the return value)

### Properties

Static properties and constants that can be accessed by any script.

| Type     | Name  | Returns  |
|----------|-------|----------|
| Constant | `E`   | `number` |
| Constant | `Pi`  | `number` |
| Constant | `Tau` | `number` |

### Functions

Standard functions that take in one or more arguments and do one thing.

| Type     | Name        | Returns  | Arguments                   |
|----------|-------------|----------|-----------------------------|
| Function | `Print`     | `void`   | `[dynamic:value]`           |
| Function | `Random`    | `number` | `[number:min] [number:max]` |
| Function | `RandomInt` | `int`    | `[int:min] [int:max]`       |

### Constructors

Constructors that return a new instance of an object. These can be called with the `new` keyword.

| Type     | Name     | Returns  | Arguments    |
|----------|----------|----------|--------------|
| Function | `Array`  | `array`  |              |
| Function | `Random` | `random` | `[int:seed]` |

### Runtime Functions

Functions that are related to the calling script or runtime.

| Type     | Name                  | Returns  | Arguments         |
|----------|-----------------------|----------|-------------------|
| Function | `GetScriptPermission` | `string` |                   |
| Function | `GetReferenceCount`   | `int`    | `[dynamic:value]` |

### String Functions

Functions that process strings.

| Type     | Name                    | Returns       | Arguments                                          |
|----------|-------------------------|---------------|----------------------------------------------------|
| Function | `StringLength`          | `int`         | `[string:str]`                                     |
| Function | `StringChars`           | `arraystring` | `[string:str]`                                     |
| Function | `StringCharAt`          | `string`      | `[string:str] [int:index]`                         |
| Function | `Substring`             | `string`      | `[string:str] [int:start] [int:length]`            |
| Function | `StringRemove`          | `string`      | `[string:str] [string:value]`                      |
| Function | `StringRemoveSubstring` | `string`      | `[string:str] [int:start] [int:length]`            |
| Function | `StringTrim`            | `string`      | `[string:str]`                                     |
| Function | `StringTrimStart`       | `string`      | `[string:str]`                                     |
| Function | `StringTrimEnd`         | `string`      | `[string:str]`                                     |
| Function | `StringReplace`         | `string`      | `[string:str] [string:oldValue] [string:newValue]` |
| Function | `StringToUpper`         | `string`      | `[string:str]`                                     |
| Function | `StringToLower`         | `string`      | `[string:str]`                                     |
| Function | `StringContains`        | `bool`        | `[string:str] [string:value]`                      |
| Function | `StringStartsWith`      | `bool`        | `[string:str] [string:value]`                      |
| Function | `StringEndsWith`        | `bool`        | `[string:str] [string:value]`                      |
| Function | `StringIndexOf`         | `int`         | `[string:str] [string:value]`                      |
| Function | `StringLastIndexOf`     | `int`         | `[string:str] [string:value]`                      |
| Function | `StringSplit`           | `arraystring` | `[string:str] [string:separator]`                  |
| Function | `ToString`              | `string`      | `[dynamic:value]`                                  |

### Type

Functions that process types.

| Type     | Name                | Returns  | Arguments                               |
|----------|---------------------|----------|-----------------------------------------|
| Function | `GetType`           | `string` | `[dynamic:value]`                       |
| Function | `GetTypeId`         | `int`    | `[dynamic:value]`                       |
| Function | `GetTypeIdFromName` | `int`    | `[string:fullTypeName]`                 |
| Function | `IsType`            | `bool`   | `[dynamic:value] [string:fullTypeName]` |
| Function | `IsTypeId`          | `bool`   | `[dynamic:value] [int:typeId]`          |

### Math

Functions that perform mathematical operations.

| Type     | Name    | Returns  | Arguments                  |
|----------|---------|----------|----------------------------|
| Function | `Abs`   | `number` | `[number:x]`               |
| Function | `Acos`  | `number` | `[number:x]`               |
| Function | `Acosh` | `number` | `[number:x]`               |
| Function | `Asin`  | `number` | `[number:x]`               |
| Function | `Asinh` | `number` | `[number:x]`               |
| Function | `Atan`  | `number` | `[number:x]`               |
| Function | `Atan2` | `number` | `[number:y] [number:x]`    |
| Function | `Atanh` | `number` | `[number:x]`               |
| Function | `Cbrt`  | `number` | `[number:x]`               |
| Function | `Ceil`  | `number` | `[number:x]`               |
| Function | `Cos`   | `number` | `[number:x]`               |
| Function | `Cosh`  | `number` | `[number:x]`               |
| Function | `Exp`   | `number` | `[number:x]`               |
| Function | `Log`   | `number` | `[number:x] [number:base]` |
| Function | `Log10` | `number` | `[number:x]`               |
| Function | `Log2`  | `number` | `[number:x]`               |
| Function | `LogE`  | `number` | `[number:x]`               |
| Function | `Max`   | `number` | `[number:x] [number:y]`    |
| Function | `Min`   | `number` | `[number:x] [number:y]`    |
| Function | `Pow`   | `number` | `[number:x] [number:y]`    |
| Function | `Round` | `number` | `[number:x]`               |
| Function | `Sign`  | `int`    | `[number:x]`               |
| Function | `Sin`   | `number` | `[number:x]`               |
| Function | `Sinh`  | `number` | `[number:x]`               |
| Function | `Sqrt`  | `number` | `[number:x]`               |
| Function | `Tan`   | `number` | `[number:x]`               |
| Function | `Tanh`  | `number` | `[number:x]`               |
| Function | `Trunc` | `number` | `[number:x]`               |