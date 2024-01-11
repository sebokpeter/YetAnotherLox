Yet another C# implementation of the Lox language from the Crafting Interpreters book (https://craftinginterpreters.com). It implements the tree-walk interpreter from the second chapter, 
and also adds some additional features on top.


- [AST Node Generation](#ast-node-generation)
- [Additional features](#additional-features)
  - [Multiline comments](#multiline-comments)
  - [Automatic string coercion](#automatic-string-coercion)
  - [Modulo](#modulo)
  - [Break and Continue](#break-and-continue)
  - [Static methods](#static-methods)
  - [Static classes](#static-classes)
  - [Arrays](#arrays)
    - [Assigning to an index](#assigning-to-an-index)
    - [Reading from an index](#reading-from-an-index)
    - [Indexing into strings](#indexing-into-strings)
  - [Compound assignment operators](#compound-assignment-operators)
- [TODOS](#todos)



# AST Node Generation

The AstGenerator project is a simple C# source generator, that takes any additional .txt files passed to the compilation, and tries to create AST nodes from them. It is used to automatically generate repetitive code for statement and expression nodes.

# Additional features

There are some additional Lox features implemented in this interpreter.

## Multiline comments

It is possible to add (nested) multiline comments:

```
/*
    This 
    is
    a 
    multiline 
    comment
    /*
        With
        Nesting
    */
    //And regular comments
*/
```

## Automatic string coercion

If one of the operands of the binary plus ('+') operator is a string, the other operand will be automatically converted to a string as well.
```
print "The answer is: " + 42;
```

## Modulo

The binary modulo operator ('%') has been added. It works the same way as in other languages:
```
print 10 % 2; // Prints 0
print 10 % 3; // Prints 1
print 12345 % 321; // Prints 147
```

## Break and Continue

**break** and **continue** statements work inside loops as expected:

```
for(var i = 0; i < 10; i = i + 1) {
    if(i % 2 == 0) {
        continue; // Skip even numbers 
    }

    print i + " is an odd number";
}
```

```
print "Counting backwards: ";
// No exit condition, will loop forever, unless 'break' is used in the body
for(var i = 0; ; i = i - 1) {
    print i;

    if(i == -10) { // Break out from the loop
        break;
    }
}
```

```
// Also works with while loops
var minimum = 10;
var maximum = 100;
var counter = 0;
while(true) {
    counter = counter + 1;

    if(counter < minimum) continue;
    if(counter == maximum) break;

    var fizzbuz;

    if(counter % 15 == 0) {
        fizzbuz = "FizzBuzz";
    } else if(counter % 3 == 0) {
        fizzbuz = "Fizz";
    } else if(counter % 5 == 0) {
        fizzbuz = "Buzz";
    } else {
        fizzbuz = counter;
    }

    print fizzbuz;
}
```
Trying to use ***break** or ****continue** outside of a loop will lead to a compiler error.

To make **continue** easier to implement, the interpreter uses a dedicated Stmt.For node, instead of desugaring to a `while` loop. This way, the loop increment can easily be executed, even when the body is skipped.

## Static methods

Added static methods. Static methods are declared with the **static** keyword are bound to the class they are declared in, not to a specific class instance. Regular classes may contain a mix of static and non-static methods. 

```
class BaseClass {
    init() {
        this.description = "Hello from base class.";
    }

    printSelf() {
        print this.description;
    }

    static printSuperStatic() {
        print "Hello from super static method";
    }
}

// Non-static class
class ClassWithStaticMethods < BaseClass {
    // Non-static initializer
    init(a, b) {
        super.init();
        this.a = a;
        this.b = b;
    }

    // Non-static method
    printSelf() {
        super.printSelf();
        print "Hello from non-static method. My members are: a = " + this.a + ", b = " + this.b + ".";
    }

    // Static method
    static printSelfStatic() {
        // It is a compile-time error to try to use 'this' inside a static method
        // print this.a; 

        print "Hello from static method";
    }
}

var c = ClassWithStaticMethods(1,2);
c.printSelf();

// Runtime error, static method cannot be called on an instance
c.printSelfStatic();
c.printSuperStatic();

// But it can be called on the class
ClassWithStaticMethods.printSelfStatic();
ClassWithStaticMethods.printSuperStatic();

// Runtime error, class has not static method named 'nonExistingStaticMethod'
ClassWithStaticMethods.nonExistingStaticMethod();
```

## Static classes

In addition to static methods, it is possible to mark an entire class as static. Static classes may only contain static methods, and they may not contain an initializer, even if it is marked static. 

```
static class StaticClass {
    // Compile-time error, static classes may not contain an initializer
    // static init() {}

    // Compile-time error, static class may not contain a non-static method
    // nonStaticMethod() {}

    static staticMethod() {
        // Compile-time error, cannot access 'this' in a static method.
        // print this.field;
    }
}
```

Additionally, static classes may not inherit from another class, and they can not be inherited from either.

```
class Base {}

// Compile-time error, a static class cannot inherit from another class
static class StaticClass < Base {} 
```

```
static class StaticBaseClass {}
// Compile-time error, a static class cannot be inherited from
class Derived < StaticBaseClass {}
```

At runtime, static classes behave as a collection of methods.

```
static class StaticMath {
    static add(a, b) {
        return a + b;
    }

    static div(a, b) {
        return a / b;
    }

    static pow(a, b) {
        var res = a;
        for(var i = 1; i < b; i = i + 1) {
            res = res * a;
        }

        return res;
    }
}

print "5 + 5 = " + StaticMath.add(5, 5);
print "15 / 3 = " + StaticMath.div(15, 3);
print "2^10 = " + StaticMath.pow(2, 10);
```

## Arrays

It is possible to create and use arrays:

```
fun printArray(array) {
    // len() is a native function that returns the number of elements in an array, or the length of a string
    for(var i = 0; i < len(array); i = i + 1) {
        write(array[i] + " "); // write() is another native functions that does not add a new line character
    }
    print "";
}

// Empty array
var array = [];

printArray(array); // Does not print anything

array[0] = "Hello";
array[1] = "Array";

printArray(array); // Prints "Hello Array"
```
Arrays may contain any type, and they can be heterogeneous:

```
var array = [];
array[0] = 0;
array[1] = "String";
array[2] = 42.42;

printArray(array);
```

Arrays can be created using one of the three approaches:

1. Use `[]` to create an empty array.
2. Specify the array elements on creation: \
   `var array = [element_1, element_2, ..., element_n]` \
   E.g.: `var array = [1, 2, 3, 4, 5]; // Will create an array with 5 elements;`
3. Use the array initializer syntax: \
   `var array = [initial_element_expression; initial_count_expression]` \
   E.g: `var array = ["Initialized"; 5];` \
   Here, there are two expression separated by a ';'. The first expression (`"Initialized"`) can evaluate to any type.
   The second expression must evaluate to a number, or a runtime error occurs. \
   The value that the first expression evaluates to will be repeated by the amount that the second expression evaluates to.
   After executing this line, the variable `array` will contain five copies of the string `"Initialized"`. Thus, the above syntax is equivalent to: ```var array = ["Initialized", "Initialized", "Initialized", "Initialized", "Initialized"]```


As arrays can contain any type, they can also contain arrays. This allows us to create jagged arrays:

```
fun displayGrid(grid) {
    for(var i = 0; i < len(grid); i = i + 1) {
        for(var j = 0; j < len(grid[i]); j = j + 1) {
            write(grid[i][j] + " | ");
        }
        print "";
    }
}

// Creates an array containing five other arrays
// Each of those five arrays will contain five zeros.
var grid = [[0;5];5];

// Prints a 5x5 grid of '0's
displayGrid(grid);

var multiplicationTable = [];

for(var i = 0; i <= 10; i = i + 1) {
    multiplicationTable[i] = [];
    for(var j = 0; j <= 10; j = j + 1) {
        multiplicationTable[i][j] = i * j;
    }
}

displayGrid(multiplicationTable);
```

### Assigning to an index

If the index is already occupied, the value will be overwritten:

```
var array = ["first"];
array[0] = "second";
print array[0]; // Prints "second"
```

If the index is larger than the current size of the array, the array will be backfilled with `nil` values, until its size is at least equal to the index.

```
array[10] = "Tenth"; // Before this line, the length of the array was 2
print array[3]; // "nil"
print array[10]; // "Tenth"
```
After the line `array[10] = "Tenth";` is executed, `array` will contain `nil` values in indices 3 through 9.

### Reading from an index

While assigning to an index that is larger than the current size of the array is valid, reading from an out-of-bounds index is not, and will cause a runtime error.

```
var array = [1, 2];
print array[0]; // Ok
print array[1]; // Ok
print array[2]; // Runtime error, index is out of bounds.
```

### Indexing into strings

The array access syntax can be used to access individual charachters of a string. Since Lox has no `char` type, each character is returned as a string.

```
var hello = "Hello";

print hello[0]; // Prints "H"
print hello[4]; // Prints "o"
```

## Compound assignment operators

Added the following compound assignment operators: `+=`, `-=`, `*=`, `/=`, `%=`. 

```
var a = 0;

a += 1;
print a; // Prints 1

a -= 11;
print a; // Prints -10

a *= -10;
print a; // Prints 100

a /= 5;
print a; // Prints 20

a %= 6;
print a; // Prints 2
```

They can be used to simplify loop increments:

```
for(var i = 0; i < 20; i += 3) {
    print i; // Print every third number.
}

// Multiply 'i' by 1.2 in each iteration
for(var i = 1; i < 100; i *= 1.2) {
    print i;
}
```

# TODOS

- Postfix operators (++, --)
- foreach loop for looping through arrays
