# Selected Photo Folder Group Title Design

## Goal

When selected unclassified photos create a work-category page, initialize its title from the folder containing the first selected photo and show that prospective title in the selection action area.

## Title Rule

- Use the immediate parent folder name of the first photo in selection order.
- If that photo is directly inside the opened base folder, use the opened base folder name.
- If no usable folder name can be derived, keep the title empty without blocking group creation.
- Photos selected from other folders do not affect the title.

## Interaction

Recompute the prospective title whenever selection order changes. Add guidance stating that the work-category title will be automatically filled with the first selected photo's folder name. On button click, assign that same value to the new group's `Title` before applying the existing `전`, `중`, `후`, and omit placement rule and saving metadata.

## Test Impact

Focus validation on a nested photo path, a photo directly under the opened base folder, and the existing four-photo placement behavior. Then build the full application to catch UI integration errors.
