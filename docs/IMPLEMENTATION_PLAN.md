The assessment is to implement a digital microwave as a web program using object orientation and .NET Framework 4.0+, separating UI and business layers.
​

There are four difficulty levels, and at least level 3 requirements must be implemented; web research is allowed, but all code will be evaluated regardless of origin.
​

General mandatory requirements: use OOP, separate layers, focus on behavior rather than UI design, and ensure the program behaves according to each level’s rules.
​

Desirable requirements: apply SOLID, design patterns, good coding practices, proper encapsulation, documentation when needed, and unit tests for the business layer.
​

Level 1 (basic behavior)
Create an interface to input time and power, via on-screen keypad and/or keyboard; tech stack for UI is up to the developer, but must integrate with C# backend (desktop or web).
​

Implement a method to start heating with configurable time and power: time between 1 second and 2 minutes; power from 1 to 10 with default 10 if omitted; times between 60 and 99 seconds must be converted and displayed as minutes:seconds (e.g., 90 → 1:30).
​

Implement validation: reject times outside the allowed range, reject power outside 0–10, and auto-fill power 10 when not informed.
​

Implement “quick start”: pressing start with no time/power starts 30 seconds at power 10. Pressing start again during heating adds 30 seconds to the remaining time.
​

During heating, display an informative string that evolves over time: character “.” repeated according to time and power (e.g., 10s at power 1 → 10 dots with spaces; 5s at power 3 → 5 groups of three dots). At the end append “Aquecimento concluído” (“Heating completed”).
​

Implement a single button for pause/cancel:

If heating is running, it pauses.

If paused and pressed again, it cancels and clears state.

If pressed before start, it clears time and power fields.
​

Level 2 (predefined programs)
Add 5 predefined heating programs with fixed name, food, time, power, heating string character, and optional usage instructions.
​

Each program must use a different heating character, not “.”, and predefined programs cannot be changed or deleted.
​

Selecting a program auto-fills time and power and locks those fields.
​

For predefined programs, adding extra time is not allowed, but pause and cancel actions still work.
​

Programs described:

Popcorn: 3 min, power 7, with instructions to stop when popping slows.

Milk: 5 min, power 5, with safety warning for liquids.

Beef: 14 min, power 4, with instruction to flip halfway.

Chicken: 8 min, power 7, flip halfway.

Beans: 8 min, power 9, uncovered, with caution for plastic containers.
​

Level 3 (custom programs)
Allow registration of custom programs with required fields: program name, food, power, heating character, and time; instructions are optional.
​

The heating character must be unique and cannot match any predefined program’s character nor the default “.”.
​

Custom programs are listed together with predefined ones but shown in italic to differentiate.
​

Persistence for custom programs may be JSON file or SQL Server.
​

Level 4 (Web API and robustness)
Expose all business operations (heating, program CRUD/usage) via a Web API with Bearer token authentication.
​

The app must show authentication status, and if auth fails, no functions can be used.
​

Credentials are configured in a dedicated screen; password field is masked; password must be stored hashed with SHA1 (256 bits as specified).
​

If using a database, the connection string must be encrypted, and documentation must show how to decrypt it.
​

Implement exception handling with a standard response format, a specific business-rule exception type, and logging of unhandled exceptions (including inner exception and stack trace) to text file or database.
​