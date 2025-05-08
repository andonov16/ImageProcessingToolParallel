# Image Processing Tool
## A WPF application for loading, transforming, and saving images from a local folder.

## Overview

- [Introduction](#introduction)
- [User Instructions](#user-instructions)
- [Code and Architecture decomposition](#code-and-architecture-decomposition)
- [Authors](#authors)


## Introduction
This project demonstrates the use of parallel and asynchronous programming in .NET to build a responsive WPF application capable of handling a large number of images (e.g., 500+). The app loads images efficiently, displays progress, and enables users to:

- Resize images to half the current thumbnail size  (e.g. as 128×128 -> 64×64)
- Convert images to grayscale
- Save transformed images to a local folder (configurable in the settings window)
- Undo up to 4 recent transformations
- Filter/search images by name
- Cancel ongoing operations (including image loading)
- Control the batch size (see Code and Architecture decomposition for more info)

## User Instructions
> [!NOTE]
> For demonstration purposes the program comes with a built-in folder with 716 images of birds sampled from the ["200 Bird Species with 11,788 Images"](#https://www.kaggle.com/datasets/veeralakrishna/200-bird-species-with-11788-images).

## Code and Architecture Decomposition

Before diving into transformation implementations and the core parallel/async logic, it’s important to understand the overall structure of the application.

### Models and Custom UI Elements

The project defines a single model, `ImageModel`, which encapsulates information about an individual image - such as its name, file path, thumbnail, and visibility status (i.e., whether it should be displayed in the UI). This model implements the `INotifyPropertyChanged` interface to enable UI data binding.

Each `ImageModel` instance is bound to a custom `UserControl` named `ThumbnailControl`. Since changes to the model can trigger UI updates, and transformations may occur on background threads, it is crucial to **freeze** each `BitmapImage` thumbnail before storing it in the model. This ensures the image can be safely accessed across threads without throwing exceptions, as WPF restricts modifications to UI objects from non-UI threads.


### Core Parallel and Async Logic - the ImageModelManager class
To maintain a clean architecture, promote code reuse, and simplify the addition of future features, we introduced the abstract class `ImageModelManager`. This class defines the core pipeline for transforming `ImageModel` instances and updating their associated thumbnails efficiently using asynchronous and parallel processing.

The key responsibilities and methods of this class are:
- **Constructor Overloads**  
  - `ImageModelManager()` – Initializes the batch size using a configuration value (`AppConfiguration["BatchSize"]`).  
  - `ImageModelManager(int batchSize)` – Allows direct initialization of a custom batch size.
- **`TransformAllImagesAsync` (public)**  
  This is the main entry point, called from the UI thread using `await`. It accepts a collection of `ThumbnailControl` instances, extracts their `ImageModel`s, and processes them in batches asynchronously. Progress is reported incrementally, and cancellation is supported via a `CancellationToken`.

- **`ApplyBatchTransformThumbnailsAsync` (protected virtual)**  
  Applies transformations to a batch of `ImageModel`s using controlled parallelism. A `SemaphoreSlim` limits concurrency to `2 x CPU cores`, and results are updated on the UI thread using `Dispatcher.InvokeAsync`. Thumbnails are updated in grouped batches for performance.

- **`GetTransformThumbnailTask` (protected abstract, the unit of work)**  
  An abstract method that returns a Task<(ImageModel, BitmapImage)> class that contains the logic for processing a single image.

By processing images in batches, we avoid creating an excessive number of `Task` instances simultaneously (in our demo scenario, this would be 716 tasks at once). Instead, images are processed in smaller groups—e.g., 32 at a time—and the UI is updated only after each batch has completed its transformations. This approach keeps the UI responsive, reduces memory consumption, and allows users to begin viewing transformed thumbnails incrementally, even before the entire image set has finished processing.


This class is then inherited by the following specialized managers:

- **`ImageModelColorManager`**  
  This class implements the `GetTransformThumbnailTask` method to transform images by changing their color to grayscale.

- **`ImageModelLoadManager`**  
  In this implementation, all three methods are slightly modified to handle image loading and UI element initialization. In addition to transforming thumbnails, this class ensures that the UI elements displaying the thumbnails are set up appropriately.

- **`ImageModelResizeManager`**  
  This class implements the `GetTransformThumbnailTask` method to resize images. It contains the logic to transform the image by changing its size while keeping the transformation process consistent with the other managers.

- **`ImageModelSaveManager`**  
  The `ImageModelSaveManager` is responsible for saving the transformed images to disk. It extends the base class by uses a custom GetSaveThumbnailAsync method since we do not return anything here. 


### Image Search
As the user types in the search bar, image visibility is updated to show only those whose names start with the search query.

The `SearchBarTextBox_TextChanged` event handler uses `AsParallel()` to efficiently update the visibility of images based on the search query.

This approach ensures quick filtering even with a large number of images while the UI remains responsive.


## Authors
- [Miroslav Andonov](https://github.com/andonov16)
- [Yana Ivanova](https://github.com/osobnyacc)