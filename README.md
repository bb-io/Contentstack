# Blackbird.io Contentstack

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->  

Contentstack is a headless CMS technology assembled from a series of best-of-breed solutions. This Contentstack application primarily centers around entry and asset management.

## Connecting

1. Navigate to apps and search for Contentstack. If you cannot find Contentstack then click _Add App_ in the top right corner, select Contentstack and add the app to your Blackbird environment.
2. Click _Add Connection_.
3. Name your connection for future reference e.g. 'My client'.
4. In the _Host_ field input one of the following values: api.contentstack.io, eu-api.contentstack.com, azure-na-api.contentstack.com, azure-eu-api.contentstack.com
5. Go to your Contentstack _Stack settings_ and copy API key from the _API Credentials_ section
6. Paste it to the _Stack API key_ field in Blackbird
7. On the settings page go to _Tokens_, select _Management Tokens_ section and generate API management token with the parameters you need.
8. Copy it to the _Management token_ field in Blackbird
9. Click _Connect_.
10. Confirm that the connection has appeared and the status is _Connected_.

## Actions

### Assets

- **Download/Upload asset** downloads or uploads content for the asset.

### Content types
- **List content types**

### Entries
- **Get entry** returns details of the specific entry.
- **Get entry string/number/date/boolean property** gets data of the specified entry's property.
- **Set entry string/number/date/boolean property** sets data to the specified entry's property.
- **Search entries** returns a list of entries based on provided filter criterias.
- **Get entry content as HTML** returns entry content converted to an HTML file.
- **Update entry content from HTML** updates entry content from HTML file.

## Events

-   **On asset deleted**
-   **On asset published**
-   **On asset unpublished**
-   **On entry created**
-   **On entry deleted**
-   **On entry published**
-   **On entry unpublished**
-   **On entry updated**

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
