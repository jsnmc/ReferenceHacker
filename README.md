ReferenceHacker
Hacks on some Visual Studio project files.

Allows references to be moved (physically move the .dll files) and inserts a global properties file into each project to make changes in the future easier.

The reason for this project was to take a large internal directory that contained all the third party references 
( i.e. log4net, antlr, spring.net, etc) and elevate it up to it's own git repo. Wanted an automated way to go through the numerous projects and update the references. 
In the process, add a global properites file ( see sample ) to each project. 
The global properties file contains an environment variable that should point to this new externals repo.

Given that the project has the following layout:

[https://github.com/jsnmc/ReferenceHacker/blob/master/docs/ProjectExisting.PNG]

The projects refernce external components in the "External" directory:

[[https://github.com/jsnmc/ReferenceHacker/blob/master/docs/ProjectExisting_references.PNG]]

The "External" directory is hoisted up to the directory of your choice - in this case, the top level directory where it can be placed in its own repo:

[[https://github.com/jsnmc/ReferenceHacker/blob/master/docs/ProjectAfter.PNG]]

The project files are "hacked" on to use the "External" location referenced by the global properties file ( typcially a .csproj file):

[[https://github.com/jsnmc/ReferenceHacker/blob/master/docs/ProjectChanges.PNG]]

Just for reference, a sample properties file is included.  This one is simple in that it only contains one property that points to a default in the even that the environment variable isn't set:

[[https://github.com/jsnmc/ReferenceHacker/blob/master/docs/ProjectReferenceFile.PNG]]

