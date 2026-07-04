# Installation:
# 1. pip install GitPython
# 2. pip install py7zr
# 3. pip install PyGithub
# 4. Add msbuild executable to PATH environment variable (For example: C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin)
# 5. Add git executable to PATH environment variable (For example: C:\Program Files\Git\bin)
# 6. Create Github API token
#       Profile -> Settings -> Developer settings -> Personal access tokens -> Tokens (classic) -> Generate new token (classic)
#       Select repo checkmark (Full control of private repositories)

import os
import git  
import shutil
import datetime
from github import Github
from github import Auth
from py7zr import pack_7zarchive

git_repo = git.Repo(os.getcwd(), search_parent_directories=True)

git_branch = git_repo.active_branch.name
if git_branch != 'main':
    exit('ERROR: You can only create a release from the main branch!')

if git_repo.is_dirty():
    exit('ERROR: You can not have uncommited changes!')

# Initialize Github connection
print('Enter Github access token:')
github_token = input()

print('Initialize Github connection')
auth = Auth.Token(github_token)
github = Github(auth=auth)
github_repo = github.get_repo('MrBama/DigitalBattleMap')

# Fetch data from git
print('Fetch data from git...')
git_repo.remotes.origin.fetch()

# Create unique release name
release_date = datetime.datetime.now().strftime('%#y.%#m.%#d')

number_of_releases = 0
for i in range(len(git_repo.tags)):
    tag = str(git_repo.tags[i])
    if release_date in tag:
        number_of_releases = number_of_releases + 1

release_version = release_date
if number_of_releases > 0:
    release_version = f'{release_version}_{number_of_releases}'

release_name = f'DigitalBattleMap_v{release_version}'
server_release_name = f'DigitalBattleMapServer_v{release_version}'

# Print overview of actions
print('\nActions:')
print('- Update version in ApplicationUpdater.cs')
print(f'- Create git tag {release_name}')
print(f'- Create {release_name}.7z')
print(f'- Create {server_release_name}.7z')
    
print('\nAre you sure you want to create a new release? (y/n)')
if input() == 'y':
    # Change ApplicationVersion
    print('Update version in ApplicationUpdater.cs')
    application_updater_path = os.path.join(os.getcwd(), "..", "DigitalBattleMap", "DigitalBattleMap", "Utilities", "ApplicationUpdater.cs")
    lines = open(application_updater_path, 'r').readlines()
    line_index = 0
    for line in lines:
        if 'ApplicationVersion =' in line:
            break
        line_index = line_index + 1
    lines[line_index] = f'    public static readonly string ApplicationVersion = "{release_version}";\n'

    with open(application_updater_path, 'w') as new_file:
        new_file.writelines(lines)

    git_repo.index.add(application_updater_path)
    commit = git_repo.index.commit(f'Update application version to v{release_version}')
    
    # Build solution
    print('Build solution...')
    errorcode = os.system(f'msbuild "{os.path.join(os.getcwd(), "..", "DigitalBattleMap", "DigitalBattleMap.sln")}" -t:restore,rebuild -p:RestorePackagesConfig=true,Configuration=Release')
    if errorcode != 0:
        exit("ERROR: Build failed!")

    # Create temp directory
    dbm_release_path = None
    server_release_path = None
    temp_directory = os.path.join(os.getcwd(), "temp")
    if os.path.exists(temp_directory):
        shutil.rmtree(temp_directory)
    shutil.register_archive_format('7zip', pack_7zarchive, description='7zip archive')

    # Create release tag
    print(f'Create release tag: {release_name}')
    git_repo.create_tag(release_name)

    # Create release 7z for DigitalBattleMap
    print(f'\nStart creation of {release_name}.7z')
    bin_directory = os.path.join(os.getcwd(), "..", "DigitalBattleMap", "DigitalBattleMap", "bin", "Release", "net6.0-windows")
    shutil.copytree(bin_directory, temp_directory, ignore=lambda directory, contents: ['runtimes'] if directory == bin_directory else [])
    shutil.copytree(os.path.join(bin_directory, "runtimes", "win"), os.path.join(temp_directory, "runtimes", "win"))
    shutil.copytree(os.path.join(bin_directory, "runtimes", "win-x64"), os.path.join(temp_directory, "runtimes", "win-x64"))
    shutil.copytree(os.path.join(bin_directory, "runtimes", "win-x86"), os.path.join(temp_directory, "runtimes", "win-x86"))
    print('Compress to archive (this can take a while)...')
    dbm_release_path = os.path.join(os.getcwd(), release_name)
    shutil.make_archive(dbm_release_path, '7zip', temp_directory)
    shutil.rmtree(temp_directory)
    print(f'Successfuly created {release_name}.7z')

    # Create release 7z for DigitalBattleMapServer
    print(f'\nStart creation of {server_release_name}.7z')
    bin_directory = os.path.join(os.getcwd(), "..", "DigitalBattleMap", "DigitalBattleMapServer", "bin", "Release", "net6.0")
    wwwroot_directory = os.path.join(os.getcwd(), "..", "DigitalBattleMap", "DigitalBattleMapServer", "wwwroot")
    print('Copy binary files...')
    shutil.copytree(bin_directory, temp_directory)
    shutil.copytree(wwwroot_directory, os.path.join(temp_directory, "wwwroot"))
    print('Compress to archive (this can take a while)...')
    server_release_path = os.path.join(os.getcwd(), server_release_name)
    shutil.make_archive(server_release_path, '7zip', temp_directory)
    shutil.rmtree(temp_directory)
    print(f'Successfuly created {server_release_name}.7z')

    # Create draft release
    print('\nCreate draft release...')
    release_notes = github_repo.generate_release_notes(release_name)
    release = github_repo.create_git_release(release_name, f'DigitalBattleMap v{release_version}', release_notes.body, draft=True)
    release.upload_asset(f'{dbm_release_path}.7z')
    release.upload_asset(f'{server_release_path}.7z')

    # Confirm release
    print('\n=========================================================================\n')
    print('Please make sure that the draft release looks okay')
    print(release.html_url)
    print('\nAre you sure you want to publish the release? (y/n)')
    if input() == 'y':
        print('Publishing release')
        git_repo.remote(name="origin").push()
        git_repo.remote(name="origin").push(release_name)
        draft_release = None
        for r in github_repo.get_releases():
            if r.draft and r.tag_name == release_name:
                draft_release = r
                break
        draft_release.update_release(name=draft_release.name, message=draft_release.body, draft=False)
        print('Release published!')
    else:
        print('Removing release')
        git_repo.head.reset(commit="HEAD~1", index=True, working_tree=False)
        git_repo.index.checkout(application_updater_path, force=True)
        git_repo.delete_tag(release_name)
        release.delete_release()

    os.remove(f'{dbm_release_path}.7z')
    os.remove(f'{server_release_path}.7z')

print('\nFINISHED')
