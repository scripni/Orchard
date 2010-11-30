﻿Feature: Blog
    In order to add blogs to my site
    As an author
    I want to create blogs and create, publish and edit blog posts

Scenario: In the admin (menu) there is a link to create a Blog
    Given I have installed Orchard
    When I go to "admin"
    Then I should see "<a href="/Admin/Blogs/Create">Blogs</a>"
    
Scenario: I can create a new blog and blog post
    Given I have installed Orchard
    When I go to "admin/blogs/create"
        And I fill in
            | name | value |
            | Routable.Title | My Blog |
        And I hit "Save"
        And I go to "my-blog"
    Then I should see "<h1[^>]*>.*?My Blog.*?</h1>"
    When I go to "admin/blogs/my-blog/posts/create"
        And I fill in
            | name | value |
            | Routable.Title | My Post |
            | Body.Text | Hi there. |
        And I hit "Publish Now"
        And I go to "my-blog/my-post"
    Then I should see "<h1[^>]*>.*?My Post.*?</h1>"
        And I should see "Hi there."

Scenario: I can create a new blog with multiple blog posts each with the same title and unique slugs are generated or given for said posts
    Given I have installed Orchard
    When I go to "admin/blogs/create"
        And I fill in
            | name | value |
            | Routable.Title | My Blog |
        And I hit "Save"
        And I go to "admin/blogs/my-blog/posts/create"
        And I fill in
            | name | value |
            | Routable.Title | My Post |
            | Body.Text | Hi there. |
        And I hit "Publish Now"
        And I go to "my-blog/my-post"
    Then I should see "<h1[^>]*>.*?My Post.*?</h1>"
        And I should see "Hi there."
    When I go to "admin/blogs/my-blog/posts/create"
        And I fill in
            | name | value |
            | Routable.Title | My Post |
            | Body.Text | Hi there, again. |
        And I hit "Publish Now"
        And I go to "my-blog/my-post-2"
    Then I should see "<h1[^>]*>.*?My Post.*?</h1>"
        And I should see "Hi there, again."
    When I go to "admin/blogs/my-blog/posts/create"
        And I fill in
            | name | value |
            | Routable.Title | My Post |
            | Routable.Slug | my-post |
            | Body.Text | Are you still there? |
        And I hit "Publish Now"
        And I go to "my-blog/my-post-3"
    Then I should see "<h1[^>]*>.*?My Post.*?</h1>"
        And I should see "Are you still there?"

Scenario: I can create a new blog and blog post and when I change the slug of the blog the path of the plog post is updated
    Given I have installed Orchard
    When I go to "admin/blogs/create"
        And I fill in
            | name | value |
            | Routable.Title | My Blog |
        And I hit "Save"
        And I go to "my-blog"
    Then I should see "<h1[^>]*>.*?My Blog.*?</h1>"
    When I go to "admin/blogs/my-blog/posts/create"
        And I fill in
            | name | value |
            | Routable.Title | My Post |
            | Body.Text | Hi there. |
        And I hit "Publish Now"
        And I go to "my-blog/my-post"
    Then I should see "<h1[^>]*>.*?My Post.*?</h1>"
        And I should see "Hi there."
    When I go to "admin/blogs/my-blog"
        And I follow "Blog Properties"
        And I fill in
            | name | value |
            | Routable.Slug | my-other-blog |
        And I hit "Save"
        And I go to "my-other-blog"
    Then I should see "<h1[^>]*>.*?My Blog.*?</h1>"
    When I go to "my-other-blog/my-post"
    Then I should see "<h1[^>]*>.*?My Post.*?</h1>"
        And I should see "Hi there."

Scenario: When viewing a blog the user agent is given an RSS feed of the blog's posts
    Given I have installed Orchard
    When I go to "admin/blogs/create"
        And I fill in
            | name | value |
            | Routable.Title | My Blog |
        And I hit "Save"
        And I go to "admin/blogs/my-blog/posts/create"
        And I fill in
            | name | value |
            | Routable.Title | My Post |
            | Body.Text | Hi there. |
        And I hit "Publish Now"
        And I am redirected
        And I go to "my-blog/my-post"
    Then I should see "<link rel="alternate" type="application/rss\+xml" title="My Blog" href="/rss\?containerid=\d+" />"

    
Scenario: Enabling remote blog publishing inserts the appropriate metaweblogapi markup into the blog's page
    Given I have installed Orchard
        And I have enabled "XmlRpc"
        And I have enabled "Orchard.Blogs.RemotePublishing"
    When I go to "admin/blogs/create"
        And I fill in
            | name | value |
            | Routable.Title | My Blog |
        And I hit "Save"
        And I go to "my-blog"
    Then I should see "<link href="[^"]*/XmlRpc/LiveWriter/Manifest" rel="wlwmanifest" type="application/wlwmanifest\+xml" />"
    When I go to "/XmlRpc/LiveWriter/Manifest"
    Then the content type should be "\btext/xml\b"
        And I should see "<manifest xmlns="http\://schemas\.microsoft\.com/wlw/manifest/weblog">"
        And I should see "<clientType>Metaweblog</clientType>"

Scenario: The virtual path of my installation when not at the root is reflected in the URL example for the slug field when creating a blog or blog post
    Given I have installed Orchard at "/OrchardLocal"
    When I go to "admin/blogs/create"
    Then I should see "<span>http\://localhost/OrchardLocal/</span>"
    When I fill in
        | name | value |
        | Routable.Title | My Blog |
        And I hit "Save"
        And I go to "admin/blogs/my-blog/posts/create"
    Then I should see "<span>http\://localhost/OrchardLocal/my-blog/</span>"

Scenario: The virtual path of my installation when at the root is reflected in the URL example for the slug field when creating a blog or blog post
    Given I have installed Orchard at "/"
    When I go to "admin/blogs/create"
    Then I should see "<span>http\://localhost/</span>"
    When I fill in
        | name | value |
        | Routable.Title | My Blog |
        And I hit "Save"
        And I go to "admin/blogs/my-blog/posts/create"
    Then I should see "<span>http\://localhost/my-blog/</span>"