﻿Feature: Blog management
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