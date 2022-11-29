# ETF-RI-CEG (Graphical software tool)

**ETF-RI-CEG** is a graphical software tool for creating cause-effect graph specifications. It was developed by using the Windows Forms desktop application type and the C# programming language. The tool is intended for helping domain experts and end users to easily create cause-effect graph representations by using an intuitive user interface and graphical cause-effect graph elements.

This application was developed at the Department of Computer Science and Informatics, Faculty of Electrical Engineering, University of Sarajevo, Bosnia and Herzegovina.

If you use this software tool for your research, please cite the following work:

```
E. Krupalija, Š. Bećirović, I. Prazina, E. Cogo and I. Bešić, "New Graphical Software Tool for Creating Cause-Effect Graph Specifications," in Journal of Communications Software and Systems, vol. 18, no. 4, pp. 311-322, November 2022, doi: 10.24138/jcomss-2022-0076
```

## Prerequisites

The application was developed by using **.NET 5**, which is cross-platform and supported on Microsoft Windows, Linux and Mac OS operating systems.
However, Windows Forms application type cannot run on other operating systems yet. If you want to use this application on other operating systems, you must use [Wine](https://www.winehq.org).

To be able to use ETF-RI-CEG on the Microsoft Windows operating system, you first need to install *.NET 5 Desktop Runtime*. You can download it from [here](https://dotnet.microsoft.com/en-us/download/dotnet/5.0) for your version of MS Windows operating system.

Users of Microsoft Windows and other operating systems with a working Wine installation can directly execute the application by accessing the following file:

```
CauseEffectGraphGraphical\CauseEffectGraph\bin\Debug\net5.0-windows\CauseEffectGraph.exe
```

 or
 
 ```
CauseEffectGraphGraphical\CauseEffectGraph\bin\Release\net5.0-windows\CauseEffectGraph.exe
```

## Functionalities

### Adding new nodes

In order to add a new node, a drag-and-drop operation needs to be performed on the desired node type button. The desired node type is then added to the panel at the location of the cursor, as shown on the image below, where the required actions for adding a new node to the graph are marked with red rectangles. The new node is always assigned the lowest unclaimed number for its type. After a new node is added to the graph, it is also added to the list of existing nodes.

![Fig. 1](https://github.com/ehlymana/ETF-RI-CEG-Graphical/blob/main/Images/Fig.%201%20Adding%20new%20nodes.png)

### Moving existing nodes

In order to move an existing node to a different location in the cause-effect graph, a drag-and-drop operation needs to be performed by selecting the desired node on the panel and moving the cursor to the new desired location. The image below shows this process (the required actions are marked with red rectangles), which is very similar to adding a new node, except it is always performed on an existing node at the panel instead of by using the desired node type button. Usage of the move operation does not change the contents of the list of existing nodes.

![Fig. 2](https://github.com/ehlymana/ETF-RI-CEG-Graphical/blob/main/Images/Fig.%202%20Moving%20existing%20nodes.png)

### Removing existing nodes

Single or multiple existing nodes can be removed from the cause-effect graph by using the list of existing nodes, where a checkbox is located on the left side of all existing nodes. After checking the boxes next to all nodes which need to be deleted from the list, clicking on the Delete button results in removing the nodes both from the panel and the list of existing nodes. The image below demonstrates the deleting process of multiple nodes (I1 and I2) from the cause-effect graph, where the required actions are marked with red rectangles. After deleting a node from the graph, all logical relations and constraints that contain this node are also deleted.

![Fig. 3](https://github.com/ehlymana/ETF-RI-CEG-Graphical/blob/main/Images/Fig.%203%20Deleting%20nodes.png)

### Adding new logical relations and constraints

Adding and removing logical relations and constraints are done in the same way for all different types in the graphical software tool. The entire process necessary for successfully adding a new constraint to the cause-effect graph in the graphical software tool is shown on the image below, where the required actions for adding a new EXC constraint to the graph are marked with red rectangles. In order to add a new constraint, the desired constraint type first needs to be selected. The Finish and Drop buttons are then added to the user interface. Depending on the type of the constraint, different types of nodes need to be selected by clicking on the desired node on the panel. After clicking on the desired node, a black box appears around the node. After selecting the desired nodes, the Finish button is clicked. The constraint is then graphically added to the cause-effect graph and shown in the list of existing constraints. If a mistake is made in the process of adding a new constraint, the Drop button can be clicked, which resets the nodes without adding the constraint to the graph.

![Fig. 4](https://github.com/ehlymana/ETF-RI-CEG-Graphical/blob/main/Images/Fig.%204%20Adding%20new%20logical%20relation.png)

### Removing existing logical relations and constraints

Removing existing logical relations and constraints in the graphical software tool works in the same way as removing an existing node. One or more relations are selected by using the checkbox in the list of defined logical relations. When the Delete button is clicked, the logical relations are deleted from the cause-effect graph shown on the panel and from the list of defined logical relations. This process is shown on the image below, where the required actions for deleting a single logical relation from the graph are marked with red rectangles.

![Fig. 5](https://github.com/ehlymana/ETF-RI-CEG-Graphical/blob/main/Images/Fig.%205%20Deleting%20existing%20logical%20relations.png)

### Import/Export feature

After defining the desired cause-effect graph, it can be saved for later usage by using the Import/Export option. This option is located in the lower right corner of the tool, as shown on the image below. Export is done by choosing the Export option, after which the user is prompted to choose the desired folder where the exported graph will be saved. The user is informed of the path of the exported graph, which is saved in the exportedData.txt file. The exported .txt file contains the structure of the graph (graph nodes, their locations, logical relations and constraints) created in the graphical software tool. The contents of the file are easily readable and can be used for importing the graph for later usage in the graphical software tool. When importing an existing exported graph file, the user is prompted to choose the desired exported file that contains the graph definition, after which the graph is shown on the panel of the tool, where it can be modified.

![Fig. 6](https://github.com/ehlymana/ETF-RI-CEG-Graphical/blob/main/Images/Fig.%206%20Import-Export.png)
