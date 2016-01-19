ReferenceHacker
Hacks on some Visual Studio project files.

Allows references to be moved (physically move the .dll files) and inserts a global properties file into each project to make changes in the future easier.

The reason for this project was to take a large internal directory that contained all the third party references 
( i.e. log4net, antlr, spring.net, etc) and elevate it up to it's own git repo. Wanted an automated way to go through the numerous projects and update the references. 
In the process, add a global properites file ( see sample ) to each project. 
The global properties file contains an environment variable that should point to this new externals repo.
